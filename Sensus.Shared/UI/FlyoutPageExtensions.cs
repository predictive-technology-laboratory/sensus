using Xamarin.Forms;

namespace Sensus.UI
{
	public static class FlyoutPageExtensions
	{
		public static void RegisterNavigationEvents(this FlyoutPage page)
		{
			page.PropertyChanging += (o, e) =>
			{
				if (e.PropertyName == nameof(page.Detail))
				{
					if (page.Detail is InputGroupPage withoutNavigationPage)
					{
						withoutNavigationPage.Interrupt();
					}
					else if (page.Detail is NavigationPage navigationPage && navigationPage.CurrentPage is InputGroupPage withNavigationPage)
					{
						withNavigationPage.Interrupt();
					}
				}
			};
		}
	}
}
