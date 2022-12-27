using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static MTUDPDispatcher.DataTypes;

namespace MTUDPDispatcher
{
    public struct ItemDefinition
    {
        public enum ItemType
        {
            ITEM_NONE,
            ITEM_NODE,
            ITEM_CRAFT,
            ITEM_TOOL,
        }

        ItemType type;
        string name;
        string description;
        string short_description;

        string inventory_image; 
        string inventory_overlay; // Overlay of inventory_image.
        string wield_image; // If empty, inventory_image or mesh (only nodes) is used
        string wield_overlay; // Overlay of wield_image.
        string palette_image; // If specified, the item will be colorized based on this
        SColor color;
        v3f wield_scale;

        ushort stack_max;
        bool usable;
        bool liquids_pointable;
        // May be NULL. If non-NULL, deleted by destructor
        ToolCapabilities* tool_capabilities;
        ItemGroupList groups;
        SimpleSoundSpec sound_place;
        SimpleSoundSpec sound_place_failed;
        SimpleSoundSpec sound_use, sound_use_air;
        f32 range;

        string node_placement_prediction;
        byte place_param2;
    }

    public struct SColor
    {
        byte A;
        byte R;
        byte G;
        byte B;
    }
}
