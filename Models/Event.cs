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
        [Required]
        public int VenueId { get; set; } // VenueId must be required for validation
        public Venue? Venue { get; set; }
        // Free-text venue name (if organizer supplies a new venue)
        [Required]
        public string VenueName { get; set; } = string.Empty;
        [Required]
        public DateTime StartDate { get; set; }
        [Required]
        public DateTime EndDate { get; set; }
        public bool IsPublished { get; set; }
        public string? OrganizerId { get; set; }
        [ForeignKey("OrganizerId")]
        public ApplicationUser? Organizer { get; set; }
        public string ImagePath { get; set; } = string.Empty; // Event image file path
    }
}