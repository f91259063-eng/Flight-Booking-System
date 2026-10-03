namespace Flight_Booking_System.Models
{
    public class AirPort
    {

        public int Id { get; set; }
        public string Name { get; set; }
        public ICollection<Route> FromRoutes { get; set; }
        = new List<Route>();

        public ICollection<Route> ToRoutes { get; set; }
            = new List<Route>();

    }
}
