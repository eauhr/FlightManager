using System.ComponentModel.DataAnnotations;

namespace FlightManager.Models
{
    public enum TypeTicket
    {
        Economy,
        Business,
    }
    public class Passenger
    {
        public int Id { get; set; }
        [Required]
        public string FirstName { get; set; }
        [Required]
        public string MiddleName { get; set; }
        [Required]
        public string LastName { get; set; }
        [Required]
        public string EGN { get; set; }
        [Required]
        public string PhoneNumber { get; set; }
        [Required]
        public string Nationality { get; set; }
        public TypeTicket Type { get; set; }
        public int ReservationId { get; set; }
        [Required]
        public Reservation Reservation { get; set; }
    }
}
