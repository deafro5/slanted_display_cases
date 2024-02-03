using Vintagestory.API.Client;
using Vintagestory.API.Server;
using Vintagestory.API.Config;
using Vintagestory.API.Common;

namespace slanted_display_cases
{
    public class slanted_display_casesModSystem : ModSystem
    {
        // Called on server and client
        // Useful for registering block/entity classes on both sides
        public override void Start(ICoreAPI api)
        {
            api.Logger.Notification("Hello from template mod: " + api.Side);
            api.RegisterBlockClass("BlockDisplayCaseSlanted", typeof(BlockDisplayCaseSlanted));
            api.RegisterBlockEntityClass("DisplayCaseSlanted", typeof(BlockEntityDisplayCaseSlanted));
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            api.Logger.Notification("Hello from template mod server side: " + Lang.Get("slanted_display_cases:hello"));
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            api.Logger.Notification("Hello from template mod client side: " + Lang.Get("slanted_display_cases:hello"));
        }
    }
}
