using System.Xml.Linq;
using UrjaAPI.Model;

namespace UrjaAPI
{
    public class StateDataSource
    {
        public List<StateDto> State { get; set; }
        public static StateDataSource current { get; } = new StateDataSource();
        public StateDataSource()
        {
            State = new List<StateDto>()
            {
                new StateDto() { Id = 1, Name = "PA" },
                new StateDto() { Id = 2, Name = "PA" },
                new StateDto() { Id = 3, Name = "PA" },
                new StateDto() { Id = 4, Name = "PA" }
            };
        }
    }
}
