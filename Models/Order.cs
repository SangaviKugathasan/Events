namespace EventZax.Models
{
    public class Order
    {
        public int Id { get; set; }
        public string UserId { get; set; } = null!;
        public int EventId { get; set; }
        public int TicketTierId { get; set; }
        public int Quantity { get; set; }
        public decimal TotalPrice { get; set; }
        public string Status { get; set; } = "Pending"; // e.g. Paid, Pending, Cancelled
        public string QrCodePath { get; set; } = string.Empty;
    }
}