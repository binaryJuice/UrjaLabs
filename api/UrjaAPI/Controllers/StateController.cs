using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using UrjaAPI.Model;

namespace UrjaAPI.Controllers
{
    [Route("api")]
    [ApiController]
    public class StateController : ControllerBase
    {

        [HttpGet]
        public ActionResult<IEnumerable<StateDto>> Get()
        {
            // why there is no NOT found here, coz empty collections is a collection
            // , it just happens to be a empty list
            return Ok(StateDataSource.current.State);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("{id}")]
        public ActionResult<StateDto> GetStateDetailsById(int id)
        {
            // linq considerations 
            // http status code =200
            //var state = StateDataSource.current.State.Where(s => s.Id == id)

            // http status code =404
            var state = StateDataSource.current.State.FirstOrDefault(s => s.Id == id);
            
            if (state==null)
                return NotFound();
            #region HowDoesThisCodeIsImprovedByAddingADataSource
            //return new JsonResult(
            //    new List<object>()
            //    {
            //        new{id=1, State="PA"},
            //        new{id=2, State="PA"},
            //        new{id=1, State="PA"},
            //        new{id=1, State="PA"},
            //    });
            #endregion
            return Ok(StateDataSource.current.State.Where(s=>s.Id==id));
        }

        #region deprecated

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <returns></returns>
        // [HttpGet("states")]
        // public JsonResult GetStateDetails()
        // {
        //     #region HowDoesThisCodeIsImprovedByAddingADataSource
        //     //return new JsonResult(
        //     //    new List<object>()
        //     //    {
        //     //        new{id=1, State="PA"},
        //     //        new{id=2, State="PA"},
        //     //        new{id=1, State="PA"},
        //     //        new{id=1, State="PA"},
        //     //    });
        //     #endregion
        //     return new JsonResult (StateDataSource.current.State);            
        // }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        //[HttpGet("{id}")]
        //public JsonResult GetStateDetailsByIdOutdated(int id)
        //{

        //    return new JsonResult(
        //        new List<object>()
        //        {
        //            new{id=1, State="PA"},
        //            new{id=2, State="PA"},
        //            new{id=1, State="PA"},
        //            new{id=1, State="PA"},
        //        });

        //    return new JsonResult(StateDataSource.current.State.Where(s => s.Id == id));
        //}
        #endregion
    }
}
