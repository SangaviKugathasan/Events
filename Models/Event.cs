using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventZax.Models
{
    public class Event
    {
        public int Id { get; set; }
        [Required]
        public string Title { get; set; } = string.Empty;
        [Required]
        public string Category { get; set; } = string.Empty;
        // Free-text venue name (organizers supply the venue name directly)
        [Required]
        public string VenueName { get; set; } = string.Empty;
        [Required]
        public DateTime StartDate { get; set; }
        // EndDate is optional because some events are single-day
        public DateTime? EndDate { get; set; }
        public bool IsPublished { get; set; }
        public string? OrganizerId { get; set; }
        [ForeignKey("OrganizerId")]
        public ApplicationUser? Organizer { get; set; }
        public string ImagePath { get; set; } = string.Empty; // Event image file path
    }
}