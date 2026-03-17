using System.ComponentModel.DataAnnotations;

namespace FlightManager.Models
{
    public class Flight
    {
        public int Id { get; set; }
        [Required]
        public string DepartureCity { get; set; }
        [Required]
        public string ArrivalCity { get; set; }
        [Required]
        public DateTime DepartureTime { get; set; }
        [Required]
        public DateTime ArrivalTime { get; set; }
        [Required]
        public string PlaneType { get; set; }
        [Required]
        public string PlaneNumber { get; set; }
        [Required]
        public string PilotName { get; set; }
        [Required]
        public int PassengersCapacity { get; set; }
        [Required]
        public int BusinessClassCapacity { get; set; }

        public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
    }
}
