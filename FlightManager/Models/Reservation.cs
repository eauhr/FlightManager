using System.ComponentModel.DataAnnotations;

namespace FlightManager.Models
{
  
    public class Reservation
    {
        public int Id { get; set; }

        [Required]
        [EmailAddress]
        public string ContactEmail { get; set; } = null!;

        public bool IsConfirmed { get; set; } = false;

        public int FlightId { get; set; }
        public Flight Flight { get; set; }
        public ICollection<Passenger> Passengers { get; set; } = new List<Passenger>();
    }
}
