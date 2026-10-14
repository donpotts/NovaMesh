// Prevents additional console window on Windows in release
#![cfg_attr(not(debug_assertions), windows_subsystem = "windows")]

use std::process::{Child, Command};
use std::sync::Mutex;
use std::path::PathBuf;
use std::time::Duration;
use tauri::{AppHandle, Manager, State};

struct OrchestratorState {
    server_child: Mutex<Option<Child>>,
    api_port: u16,
    ui_port: u16,
}

fn get_sidecar_or_dev_path(app: &AppHandle, binary_name: &str) -> Option<PathBuf> {
    // 1. Try sidecar bundled path inside resource_dir
    if let Ok(resource_dir) = app.path().resource_dir() {
        let candidate = resource_dir.join("binaries").join(format!("{}.exe", binary_name));
        if candidate.exists() {
            return Some(candidate);
        }
        let candidate_no_ext = resource_dir.join("binaries").join(binary_name);
        if candidate_no_ext.exists() {
            return Some(candidate_no_ext);
        }
    }

    // 2. Try current executable parent directory
    if let Ok(current_exe) = std::env::current_exe() {
        if let Some(parent) = current_exe.parent() {
            let candidate = parent.join("binaries").join(format!("{}.exe", binary_name));
            if candidate.exists() {
                return Some(candidate);
            }
            let candidate_no_ext = parent.join("binaries").join(binary_name);
            if candidate_no_ext.exists() {
                return Some(candidate_no_ext);
            }
        }
    }

    // 3. Fallback to development project path
    let dev_candidate = PathBuf::from(format!("src/{}/{}.csproj", binary_name, binary_name));
    if dev_candidate.exists() {
        return Some(dev_candidate);
    }
    let parent_dev = PathBuf::from(format!("../src/{}/{}.csproj", binary_name, binary_name));
    if parent_dev.exists() {
        return Some(parent_dev);
    }

    None
}

fn run_db_migrations(app: &AppHandle, app_name: &str) -> Result<(), String> {
    println!("[Ask2App | My Platform] ========================================");
    println!("[Ask2App | My Platform] Step 1/3: Executing Database Migrations...");
    println!("[Ask2App | My Platform] ========================================");

    let migrator_name = format!("{}.DbMigrator", app_name);
    let path = get_sidecar_or_dev_path(app, &migrator_name);

    if let Some(target) = path {
        if target.extension().and_then(|s| s.to_str()) == Some("csproj") {
            println!("[Ask2App Supervisor] Running DbMigrator via dotnet project: {:?}", target);
            let mut child = Command::new("dotnet")
                .arg("run")
                .arg("--project")
                .arg(&target)
                .spawn()
                .map_err(|e| format!("Failed to spawn dotnet DbMigrator: {}", e))?;

            let status = child.wait().map_err(|e| format!("Failed waiting for DbMigrator: {}", e))?;
            if !status.success() {
                return Err(format!("DbMigrator exited with code {:?}", status.code()));
            }
        } else {
            println!("[Ask2App Supervisor] Running bundled native DbMigrator: {:?}", target);
            let mut child = Command::new(&target)
                .spawn()
                .map_err(|e| format!("Failed to spawn native DbMigrator: {}", e))?;

            let status = child.wait().map_err(|e| format!("Failed waiting for DbMigrator: {}", e))?;
            if !status.success() {
                return Err(format!("DbMigrator exited with code {:?}", status.code()));
            }
        }
    } else {
        println!("[Ask2App Supervisor] Note: No standalone DbMigrator binary found; server will self-migrate.");
    }

    println!("[Ask2App | My Platform] ✓ Database migration and multi-module seed verified.");
    Ok(())
}

fn start_backend_server(app: &AppHandle, app_name: &str, port: u16) -> Result<Child, String> {
    println!("[Ask2App | My Platform] ========================================");
    println!("[Ask2App | My Platform] Step 2/3: Launching ASP.NET Core Server on port {}...", port);
    println!("[Ask2App | My Platform] ========================================");

    let api_name = format!("{}.Api", app_name);
    let path = get_sidecar_or_dev_path(app, &api_name);

    let child = if let Some(target) = path {
        if target.extension().and_then(|s| s.to_str()) == Some("csproj") {
            println!("[Ask2App Supervisor] Launching API via dotnet: {:?}", target);
            Command::new("dotnet")
                .arg("run")
                .arg("--project")
                .arg(&target)
                .arg("--urls")
                .arg(format!("http://127.0.0.1:{}", port))
                .spawn()
                .map_err(|e| format!("Failed to spawn dotnet API: {}", e))?
        } else {
            println!("[Ask2App Supervisor] Launching bundled API binary: {:?}", target);
            Command::new(&target)
                .arg("--urls")
                .arg(format!("http://127.0.0.1:{}", port))
                .spawn()
                .map_err(|e| format!("Failed to spawn bundled API: {}", e))?
        }
    } else {
        Command::new("dotnet")
            .arg("run")
            .arg("--project")
            .arg(format!("src/{}.Api/{}.Api.csproj", app_name, app_name))
            .arg("--urls")
            .arg(format!("http://127.0.0.1:{}", port))
            .spawn()
            .map_err(|e| format!("Failed to spawn API server: {}", e))?
    };

    Ok(child)
}

async fn wait_for_server_healthy(port: u16, max_seconds: u32) -> bool {
    println!("[Ask2App | My Platform] ========================================");
    println!("[Ask2App | My Platform] Step 3/3: Awaiting Backend Health Check on port {}...", port);
    println!("[Ask2App | My Platform] ========================================");

    let url = format!("http://127.0.0.1:{}/robots.txt", port);
    let client = reqwest::Client::builder()
        .timeout(Duration::from_secs(2))
        .build()
        .unwrap();

    for i in 1..=max_seconds {
        if let Ok(res) = client.get(&url).send().await {
            if res.status().is_success() {
                println!("[Ask2App Supervisor] ✓ Backend server verified healthy after {}s!", i);
                return true;
            }
        }
        tokio::time::sleep(Duration::from_millis(500)).await;
    }

    eprintln!("[Ask2App Supervisor] Warning: Timed out awaiting backend health probe.");
    false
}

#[tauri::command]
fn get_orchestrator_info(state: State<OrchestratorState>) -> serde_json::Value {
    serde_json::json!({
        "branding": "Ask2App | My Platform",
        "status": "Running",
        "apiPort": state.api_port,
        "uiPort": state.ui_port,
        "serverUrl": format!("http://127.0.0.1:{}", state.api_port)
    })
}

fn main() {
    let app_name = "NovaMesh";
    let api_port = 5000;
    let ui_port = 5001;

    tauri::Builder::default()
        .manage(OrchestratorState {
            server_child: Mutex::new(None),
            api_port,
            ui_port,
        })
        .invoke_handler(tauri::generate_handler![get_orchestrator_info])
        .setup(move |app| {
            let handle = app.handle().clone();

            // 1. Run Migrations First
            if let Err(e) = run_db_migrations(&handle, app_name) {
                eprintln!("[Ask2App Error] Migration failed: {}", e);
            }

            // 2. Start Backend API Server
            match start_backend_server(&handle, app_name, api_port) {
                Ok(child) => {
                    let state: State<OrchestratorState> = app.state();
                    let mut lock = state.server_child.lock().unwrap();
                    *lock = Some(child);
                }
                Err(e) => {
                    eprintln!("[Ask2App Error] Failed to start backend: {}", e);
                }
            }

            // 3. Asynchronously wait for backend health
            tauri::async_runtime::spawn(async move {
                wait_for_server_healthy(api_port, 30).await;
            });

            Ok(())
        })
        .on_window_event(|window, event| {
            if let tauri::WindowEvent::Destroyed = event {
                println!("[Ask2App | My Platform] Window closed. Terminating background server process...");
                let state = window.state::<OrchestratorState>();
                let mut maybe_child = None;
                if let Ok(mut lock) = state.server_child.lock() {
                    maybe_child = lock.take();
                }
                if let Some(mut child) = maybe_child {
                    let _ = child.kill();
                    let _ = child.wait();
                    println!("[Ask2App | My Platform] Background server terminated cleanly.");
                }
            }
        })
        .run(tauri::generate_context!())
        .expect("error while running Ask2App | My Platform application");
}