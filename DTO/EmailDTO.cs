namespace ApiGestion.DTO
{
    public class EmailDTO
    {
        public int Id { get; set; }
        public string Subject { get; set; }

        public string Body { get; set; }

        public string AddressTo { get; set; }
    }
}
