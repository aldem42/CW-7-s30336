using System;
using System.Collections.Generic;

namespace TravelAgency.Models.DTOs
{
    public class TripDTO
    {
        public int IdTrip { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime DateFrom { get; set; }
        public DateTime DateTo { get; set; }
        public int MaxPeople { get; set; }

        public DateTime? RegisteredAt { get; set; }
        public DateTime? PaymentDate { get; set; }

        public List<CountryDTO> Countries { get; set; }
    }
}