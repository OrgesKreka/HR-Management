using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HRApplication.Models
{
    public class User
    {
        public int UserId { get; set; }
        public string Password { get; set; }
        public string UserName { get; set; }
        public byte[] ProfileImage { get; set; }
        public int SessionCode { get; set; }
        public int SessionId { get; set; }
        public TurnStatus TurnStatus { get; set; }
        public new string ToString
        {
            get
            {
                return $"UserId={UserId} : UserName={UserName} : Password={Password} : ProfileImage={ProfileImage} : SessionCode={SessionCode}";
            }
        }
    }
}
