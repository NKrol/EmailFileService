using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmailFileService.Entities
{
    public abstract class EntityBase
    {
        public int Id { get; set; }
        public DateTime AddDate { get; private set; } = DateTime.Now;
        public DateTime? LastUpdate { get; set; }
        public OperationType OperationType { get; set; } = OperationType.Create;


        public void Remove()
        {
            LastUpdate = DateTime.Now;
            OperationType = OperationType.Delete;
        }

    }

    public enum OperationType
    {
        Create,
        Delete,
        Modify
    }
}
