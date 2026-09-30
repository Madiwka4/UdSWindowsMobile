namespace HelloWorldUWP.Modules.Mensa.Models
{
    public class Canteen
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string ShortName { get; set; }
        public string City { get; set; }
        public string Address { get; set; }

        public override string ToString()
        {
            return ShortName ?? Name;
        }
    }
}
