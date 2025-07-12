namespace UrjaAPI.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using UrjaAPI.Model;

    /// <summary>
    /// Controller is about number of business interest , in a state
    /// </summary>
    [Route("api/city/{cityid}/pointofinterest")]
    [ApiController]
    public class StatePointOfInterestController : ControllerBase
    {
        /// <summary>
        /// Get all the point of interests in a particular state.
        /// </summary>
        /// <param name="stateid"> unique id of the state.</param>
        /// <returns>IEnumerable.<PointOfInterestDto></returns>
        [HttpGet]
        public ActionResult<IEnumerable<PointOfInterestDto>> Get(int stateid)
        {
            var state = StateDataSource.Current.State.FirstOrDefault(s => s.Id == stateid);
            if (state == null)
            {
                return this.NotFound();
            }

            if (state.CountPointOfInterest > 0)
            {
                return this.Ok(state.PointOfInterestDtos);
            }

            return this.Ok(state.PointOfInterestDtos);
        }

        /// <summary>
        /// Get the specific point of interest in a state
        /// </summary>
        /// <param name="cityid"> unique id of city.</param>
        /// <param name="stateid"> unique id of state.</param>
        /// <returns>PointOfInterestDto.</returns>
        [HttpGet("{stateid}")]
        public ActionResult<PointOfInterestDto> GetPointOfInterestInStateById(int cityid, int stateid)
        {
            var city = StateDataSource.Current.State.FirstOrDefault(s => s.Id == cityid);

            if (city == null)
            {
                return this.NotFound();
            }

            var pointOfInterest = city.PointOfInterestDtos.FirstOrDefault(p => p.Id == stateid);

            if (pointOfInterest == null)
            {
                return this.NotFound();
            }

            return this.Ok(pointOfInterest);
        }
    }
}
