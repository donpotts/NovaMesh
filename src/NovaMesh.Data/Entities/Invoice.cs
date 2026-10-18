namespace NovaMesh.Data.Entities;

public class Invoice
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public decimal Amount { get; set; } = 0m;
    public bool IsPaid { get; set; } = true;
    public string IssuedAt { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}