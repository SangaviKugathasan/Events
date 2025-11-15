namespace EventZax.Models
{
    public class CartItem
    {
        public int Id { get; set; }
        public int CartId { get; set; }
        public Cart Cart { get; set; } = null!;
        public int TicketTierId { get; set; }
        public TicketTier TicketTier { get; set; } = null!;
        public int Quantity { get; set; }
    }
}