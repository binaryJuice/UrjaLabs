// <copyright file="StateController.cs" company="binaryJuice">
// Copyright (c) binaryJuice. All rights reserved.
// </copyright>

namespace UrjaAPI.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using UrjaAPI.Model;

    /// <summary>
    /// Endpoint for states controller
    /// </summary>
    [Route("api")]
    [ApiController]
    public class StateController : ControllerBase
    {
        /// <summary>
        /// Get all endpoint.
        /// </summary>
        /// <returns>ActionResult.<IEnumerable<StateDto>></returns>
        [HttpGet]
        public ActionResult<IEnumerable<StateDto>> GetAllStates()
        {
            // why there is no NOT found here, coz empty collections is a collection
            // , it just happens to be a empty list
            return this.Ok(StateDataSource.Current.State);
        }

        /// <summary>
        /// GetStateDetailsById by id , this takes a parameter.
        /// </summary>
        /// <param name="id">id of the state.</param>
        /// <returns>StateDto.</returns>
        [HttpGet("{id}")]
        public ActionResult<StateDto> GetStateDetailsById(int id)
        {
            // linq considerations.
            // http status code =200.
            // var state = StateDataSource.current.State.Where(s => s.Id == id).

            // http status code =404.
            var state = StateDataSource.Current.State.FirstOrDefault(s => s.Id == id);

            if (state == null)
            {
                return this.NotFound();
            }
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

            return this.Ok(StateDataSource.Current.State.Where(s => s.Id == id));
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
