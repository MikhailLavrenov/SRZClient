using System;
using System.Collections.Generic;

namespace SrzClient
{
    public class Person
    {
        public int? PersonId { get; set; }
        public int? PolicyId { get; set; }
        public string ENP { get; set; }
        public string Surname { get; set; }
        public string Name { get; set; }
        public string Patronymic { get; set; }
        public string Sex { get; set; }
        public DateTime? Birthdate { get; set; }
        public string Birthplace { get; set; }
        public string Privilege { get; set; }
        public string Snils { get; set; }
        public Address RegAddress { get; set; }
        public Address FactAddress { get; set; }
        public string Phone { get; set; }
        public int? CitizenshipCode { get; set; }
        public int? DocumentKindCode { get; set; }
        public string DocumentSer { get; set; }
        public string DocumentNum { get; set; }
        public DateTime? DocumentDate { get; set; }
        public string PolicyCompany{ get; set; }
        public string PolicyRegistration { get; set; }
        public string Job { get; set; }
        public string PolicyKind { get; set; }
        public DateTime? PolicyValidFrom { get; set; }
        public DateTime? PolicyModify { get; set; }
        public DateTime? PolicyValidTo { get; set; }
        public string PolicyOkato { get; set; }
        public string Status { get; set; }

        public List<Policy> Policies { get; set; }

        public Person()
        {
            Policies = new List<Policy>();
        }

        public override string ToString()
        {
            return this.PropertiesToString(Environment.NewLine);
        }

    }
}
