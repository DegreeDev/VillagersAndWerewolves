using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Werewolves.Hubs;

namespace Werewolves.Controllers
{
	public class HomeController : HubController<ChatHub>
	{
		public ActionResult Index()
		{
			return View();
		}
		public ActionResult Chat()
		{
			return View();
		}
		public async Task<ActionResult> ChatKo()
		{
			await Hub.Clients.All.message("System", "Somebody viewed the ChatKo page");
			return View("Chat.ko");
		}
	}

	public class HubController<T>: Controller where T : ChatHub
	{
		public readonly Microsoft.AspNet.SignalR.IHubContext Hub; 

		public HubController()
		{
			Hub = Microsoft.AspNet.SignalR.GlobalHost.ConnectionManager.GetHubContext<T>();
		}
		protected override void OnActionExecuted(ActionExecutedContext filterContext)
		{
			Hub.Clients.Group("logger.page").message("Controller: " + filterContext.Controller + " Method: " + filterContext.ActionDescriptor.ActionName);
			base.OnActionExecuted(filterContext);
		}
	}
}
