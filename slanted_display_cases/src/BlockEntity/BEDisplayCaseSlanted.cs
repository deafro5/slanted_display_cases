using System;
using System.Text;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace slanted_display_cases
{
    public class BlockEntityDisplayCaseSlanted : BlockEntityDisplay, IRotatable
    {
        public override string InventoryClassName => "displaycaseslanted";
        protected InventoryGeneric inventory;
        public override InventoryBase Inventory => inventory;

        bool haveCenterPlacement;
        float[] rotations = new float[4];

        public BlockEntityDisplayCaseSlanted()
        {
            inventory = new InventoryDisplayed(this, 4, "displaycaseslanted-0", null, null);
        }

        internal bool OnInteract(IPlayer byPlayer, BlockSelection blockSel)
        {
            ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;
            

            if (slot.Empty)
            {
                if (TryTake(byPlayer, blockSel))
                {
                    return true;
                }
                return false;
            }
            else
            {
                CollectibleObject colObj = slot.Itemstack.Collectible;
                if (colObj.Attributes != null && colObj.Attributes["displaycaseable"].AsBool(false) == true)
                {
                    AssetLocation sound = slot.Itemstack?.Block?.Sounds?.Place;

                    if (TryPut(slot, blockSel, byPlayer))
                    {
                        Api.World.PlaySoundAt(sound != null ? sound : new AssetLocation("game:sounds/player/build"), byPlayer.Entity, byPlayer, true, 16);
                        return true;
                    }

                    return false;
                }

                (Api as ICoreClientAPI)?.TriggerIngameError(this, "doesnotfit", Lang.Get("This item does not fit into the display case."));
                return true;
            }
        }



        private bool TryPut(ItemSlot slot, BlockSelection blockSel, IPlayer player)
        {
            int index = blockSel.SelectionBoxIndex;
            bool nowCenterPlacement = inventory.Empty && Math.Abs(blockSel.HitPosition.X - 0.5f) < 0.1 && Math.Abs(blockSel.HitPosition.Z - 0.5f) < 0.1;

            var attr = slot.Itemstack.ItemAttributes;
            float height = attr?["displaycase-slanted"]["minHeight"]?.AsFloat(0.25f) ?? 0;
            if (height > (this.Block as BlockDisplayCase)?.height)
            {
                (Api as ICoreClientAPI)?.TriggerIngameError(this, "tootall", Lang.Get("This item is too tall to fit in this display case."));
                return false;
            }


            haveCenterPlacement = nowCenterPlacement;

            if (inventory[index].Empty)
            {
                int moved = slot.TryPutInto(Api.World, inventory[index]);

                if (moved > 0)
                {
                    BlockPos targetPos = blockSel.DidOffset ? blockSel.Position.AddCopy(blockSel.Face.Opposite) : blockSel.Position;
                    double dx = player.Entity.Pos.X - (targetPos.X + blockSel.HitPosition.X);
                    double dz = (float)player.Entity.Pos.Z - (targetPos.Z + blockSel.HitPosition.Z);
                    float angleHor = (float)Math.Atan2(dx, dz);
                    float deg90 = GameMath.PIHALF;
                    rotations[index] = (int)Math.Round(angleHor / deg90) * deg90;

                    updateMeshes();
                    
                    MarkDirty(true);
                }
                
                return moved > 0;
            }

            return false;
        }

        private bool TryTake(IPlayer byPlayer, BlockSelection blockSel)
        {
            int index = blockSel.SelectionBoxIndex;
            if (haveCenterPlacement)
            {
                for (int i = 0; i < inventory.Count; i++)
                {
                    if (!inventory[i].Empty) index = i;
                }
            }

            if (!inventory[index].Empty)
            {
                ItemStack stack = inventory[index].TakeOut(1);
                if (byPlayer.InventoryManager.TryGiveItemstack(stack))
                {
                    AssetLocation sound = stack.Block?.Sounds?.Place;
                    Api.World.PlaySoundAt(sound != null ? sound : new AssetLocation("sounds/player/build"), byPlayer.Entity, byPlayer, true, 16);
                }

                if (stack.StackSize > 0)
                {
                    Api.World.SpawnItemEntity(stack, Pos.ToVec3d().Add(0.5, 0.5, 0.5));
                }

                updateMesh(index);
                MarkDirty(true);
                return true;
            }

            return false;
        }

        
        BlockFacing getFacing()
        {
            Block block = Api.World.BlockAccessor.GetBlock(Pos);
            BlockFacing facing = BlockFacing.FromCode(block.LastCodePart());
            return facing == null ? BlockFacing.NORTH : facing;
        }
        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder sb)
        {
            base.GetBlockInfo(forPlayer, sb);

            sb.AppendLine();

            if (forPlayer?.CurrentBlockSelection == null) return;

            int index = forPlayer.CurrentBlockSelection.SelectionBoxIndex;
            if (index >= inventory.Count) return; // Why can this happen o.O

            if (!inventory[index].Empty)
            {
                sb.AppendLine(inventory[index].Itemstack.GetName());
            }
        }

        protected override float[][] genTransformationMatrices()
        {
            float[][] tfMatrices = new float[4][];
            BlockFacing facing = getFacing();
            int axis = facing.HorizontalAngleIndex; // E=0, N=1, W=2, S=3
            for (int index = 0; index < 4; index++)
            {
                /* Collision Boxes on Display indexes:
                    o---+---+      o = origin (changes depening on type.)
                    | 0 | 1 |
                    +---+---+
                    | 2 | 3 |
                    +---+---+
                */
                float x = 0;
                float y = 0;
                float z = 0;

                if (axis == 3){ // South
                    x = (index % 2 == 1) ? 5 / 16f : 11 / 16f;
                    z = (index > 1) ? 6 / 16f : 11f / 16f;
                    y = (float)GameMath.Tan(22.5) * z;
                }else if (axis == 1){ // North
                    x = (index % 2 == 1) ? 11 / 16f : 5 / 16f;
                    z = (index > 1) ? 11 / 16f : 6 / 16f;
                    float z1 = (index > 1) ? 6 / 16f : 11 / 16f; 
                    y = (float)GameMath.Tan(22.5) * z1;
                }else if (axis == 0){ // East
                    z = (index % 2 == 0) ? 5 / 16f : 11 / 16f;
                    x = (index > 1) ? 6 / 16f : 11f / 16f;
                    y = (float)GameMath.Tan(22.5) * x;
                }else if (axis == 2){ // West
                    z = (index % 2 == 0) ? 11 / 16f : 5 / 16f;
                    x = (index > 1) ? 11 / 16f : 6 / 16f;
                    float x1 = (index > 1) ? 6 / 16f : 11 / 16f;
                    y = (float)GameMath.Tan(22.5) * x1;
                }
                // For all cases, adjust the top row of items slightly down
                y = (index > 1 ) ? y: y - (1.01f / 16f);

                int rnd = GameMath.MurmurHash3Mod(Pos.X, Pos.Y + index * 50, Pos.Z, 30) - 15;
                var collObjAttr = inventory[index]?.Itemstack?.Collectible?.Attributes;
                if (collObjAttr != null && collObjAttr["randomizeInDisplayCase"].AsBool(true) == false)
                {
                    rnd = 0;
                }
                
                float degY = rotations[index]*GameMath.RAD2DEG + 45 + rnd;
                float degX = 0;
                float degZ = 0;
                switch(axis){
                    case 0: // East
                        degZ = 22.5f; 
                        break;
                    case 1: // North
                        degX = 22.5f;
                        break;
                    case 2: // West
                        degZ = -22.5f;
                        break;
                    case 3: // South
                        degX = -22.5f;
                        break;
                }
                

                if (haveCenterPlacement)
                {
                    x = 8 / 16f;
                    z = 8 / 16f;
                    y = x * GameMath.Tan(22.5f) - (1.01f / 16f);
                }

                

                tfMatrices[index] = 
                    new Matrixf()
                    .Translate(0.5f, 0.5f, 0.5f) // goto origin (center)
                    .Translate(x - 0.5f, y - 0.5f, z - 0.5f) // goto correct quadrant centerish
                    .RotateZDeg(degZ)
                    .RotateXDeg(degX)
                    .RotateYDeg(degY)
                    .Scale(0.75f, 0.75f, 0.75f)
                    .Translate(-0.5f, 0, -0.5f)
                    .Values
                ;
            }

            return tfMatrices;
        }

        public override void FromTreeAttributes(Vintagestory.API.Datastructures.ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAttributes(tree, worldForResolving);

            haveCenterPlacement = tree.GetBool("haveCenterPlacement");
            rotations = new float[]
            {
                tree.GetFloat("rotation0"),
                tree.GetFloat("rotation1"),
                tree.GetFloat("rotation2"),
                tree.GetFloat("rotation3"),
            };
        }

        public override void ToTreeAttributes(Vintagestory.API.Datastructures.ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);

            tree.SetBool("haveCenterPlacement", haveCenterPlacement);
            tree.SetFloat("rotation0", rotations[0]);
            tree.SetFloat("rotation1", rotations[1]);
            tree.SetFloat("rotation2", rotations[2]);
            tree.SetFloat("rotation3", rotations[3]);
        }
        
        public void OnTransformed(IWorldAccessor worldAccessor,ITreeAttribute tree, int degreeRotation, Dictionary<int, AssetLocation> oldBlockIdMapping, Dictionary<int, AssetLocation> oldItemIdMapping, EnumAxis? flipAxis)
        {
            var rot = new int[]{0, 1, 3, 2};
            var rots = new float[4];
            var treeAttribute = tree.GetTreeAttribute("inventory");
            inventory.FromTreeAttributes(treeAttribute);
            var inv = new ItemSlot[4];
            var start = (degreeRotation / 90) % 4;

            for (var i = 0; i < 4; i++)
            {
                rots[i] = tree.GetFloat("rotation" + i);
                inv[i] = inventory[i];
            }
            
            for (var i = 0; i < 4; i++)
            {
                var index = GameMath.Mod(i - start, 4);
                // swap inventory and rotations with the new ones
                rotations[rot[i]] = rots[rot[index]] - degreeRotation * GameMath.DEG2RAD;
                inventory[rot[i]] = inv[rot[index]];
                tree.SetFloat("rotation"+rot[i], rotations[rot[i]]);
            }

            inventory.ToTreeAttributes(treeAttribute);
            tree["inventory"] = treeAttribute;
        }
    }
}
