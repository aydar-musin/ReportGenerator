using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ReportGenerator
{
    class Order
    {
        public string CustomerEmail { get; set; }
        public string CompanyINNOGRN { get; set; }
        public string CompanyId { get; set; }
        public DateTime TimeStamp { get; set; }

        public override int GetHashCode()
        {
            return CustomerEmail.GetHashCode() + CompanyINNOGRN.GetHashCode();
        }
        public override bool Equals(object obj)
        {
            if (obj is Order)
            {
                return (obj as Order).GetHashCode() == this.GetHashCode();
            }
            return false;
        }
    }
}
