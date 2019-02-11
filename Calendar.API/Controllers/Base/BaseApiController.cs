using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Calendar.API.Controllers.Base
{
    [ApiController]
    [Route("v{version:apiVersion}/[controller]")]
    public abstract class BaseApiController : ControllerBase
    {

    }
}