using Kara.Core;
using Kara.Core.Input;
using Kara.Core.Visual;
using System.Drawing;

namespace Kara.Testing
{
	public class TestingApplication : Application
	{
		private View ParentingView = new Parenting();
		private View PrettyView = new PrettyUi();

		public TestingApplication()
		{
			this.Events.Access = EventAccess.Keyboard;

			AddView(ParentingView);
			AddView(PrettyView);

			SetActiveView(PrettyView);
		}
	}
}