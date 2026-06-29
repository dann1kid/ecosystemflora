using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem.Testing
{
    internal static class EcologyTestBlocks
    {
        public static Block[] CreateCatalog()
        {
            var blocks = new Block[9];
            blocks[0] = Make("game:air", b => b.BlockMaterial = EnumBlockMaterial.Air);
            blocks[1] = Make("game:soil-medium-normal", b =>
            {
                b.BlockMaterial = EnumBlockMaterial.Soil;
                b.Fertility = 150;
                b.Replaceable = 100;
                b.SideSolid[BlockFacing.UP.Index] = true;
            });
            blocks[2] = Make("game:flower-catmint-free", b =>
            {
                b.BlockMaterial = EnumBlockMaterial.Plant;
                b.Replaceable = 3000;
            });
            blocks[3] = Make("ecosystemflora:juvenile-flower-catmint-free", b =>
            {
                b.BlockMaterial = EnumBlockMaterial.Plant;
                b.Replaceable = 3000;
            });
            blocks[4] = Make("game:tallgrass-veryshort-free", b =>
            {
                b.BlockMaterial = EnumBlockMaterial.Plant;
                b.Replaceable = 3000;
            });
            blocks[5] = Make("game:tallgrass-short-free", b =>
            {
                b.BlockMaterial = EnumBlockMaterial.Plant;
                b.Replaceable = 3000;
            });
            blocks[6] = Make("game:tallgrass-tall-free", b =>
            {
                b.BlockMaterial = EnumBlockMaterial.Plant;
                b.Replaceable = 3000;
            });
            blocks[7] = Make("game:tallgrass-verytall-free", b =>
            {
                b.BlockMaterial = EnumBlockMaterial.Plant;
                b.Replaceable = 3000;
            });
            blocks[8] = Make("game:flower-cowparsley-free", b =>
            {
                b.BlockMaterial = EnumBlockMaterial.Plant;
                b.Replaceable = 3000;
            });
            blocks = Expand(blocks, Make("game:wildvine-end-north", b =>
            {
                b.BlockMaterial = EnumBlockMaterial.Plant;
                b.Replaceable = 3000;
            }));
            blocks = Expand(blocks, Make("game:wildvine-section-north", b =>
            {
                b.BlockMaterial = EnumBlockMaterial.Plant;
                b.Replaceable = 3000;
            }));

            for (int i = 0; i < blocks.Length; i++)
            {
                blocks[i].BlockId = i;
            }

            return blocks;
        }

        static Block Make(string code, System.Action<Block> configure)
        {
            var block = new Block { Code = new AssetLocation(code) };
            configure(block);
            return block;
        }

        static Block[] Expand(Block[] blocks, Block block)
        {
            var expanded = new Block[blocks.Length + 1];
            for (int i = 0; i < blocks.Length; i++)
            {
                expanded[i] = blocks[i];
            }

            expanded[blocks.Length] = block;
            return expanded;
        }
    }
}
