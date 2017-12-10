using Microsoft.EntityFrameworkCore;

namespace ODataAspNetCorePoc.Models
{
    public partial class NorthwindSlimContext
    {
        public NorthwindSlimContext(DbContextOptions<NorthwindSlimContext> options) : base(options) { }
    }
}
