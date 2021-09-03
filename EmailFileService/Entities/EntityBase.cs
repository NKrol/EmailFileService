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
        public bool IsActive { get; set; } = true;

        public void Remove()
        {
            IsActive = false;
            LastUpdate = DateTime.Now;
            OperationType = OperationType.Delete;
        }

        public void Move()
        {
            IsActive = false;
            LastUpdate = DateTime.Now;
            OperationType = OperationType.Move;
        }

    }

    public enum OperationType
    {
        Create,
        Delete,
        Modify,
        Overwrite,
        Move
    }
}
