using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Diablo2RProject.Diablo
{
    internal class DiabloAnimation: TAnimation
    {
        struct LAY_INF_S
        {
            byte shad_a;
            byte shad_b;
            byte trans_a;
            byte trans_b;
            string wclass;

            // editor only
            int bmp_num;
            //BITMAP** bmp;
            int off_x;
            int off_y;
            int last_good_frame;
        }

        public string Token;
        public string Mode;
        public string ClassType;
        public string BasePath;
        public int PaletteIdx;
        byte LayersCount;
        byte FramesCount;
        byte DirectionCount;
        int xoffset;
        int yoffset;
        LAY_INF_S[] lay_inf;
        byte[] priority;
        int cur_frame;
        int cur_dir;
        int spd_mul;
        int spd_div;
        int spd_mod; // = is (mul % div), for extra precision
        int orderflag; // from data\global\excel\objects.txt, 0 1 or 2
        byte[] Palette;
        public List<string> Armor = new List<string>();
        static string[] LayerType = {
            "HD", "TR", "LG", "RA", "LA", "RH", "LH", "SH",
            "S1", "S2", "S3","S4", "S5", "S6", "S7", "S8"};

        void ReadDcc(Stream s)
        {

        }

        void ReadDc6(Stream s)
        {

        }

        public void Read()
        {
            Name = $"{Token}{Mode}{ClassType}";
            var basePath = $"{TDiabloMap.GamePath}/D2/data/global/{BasePath}/{Token}/";
            var fileName = $"{basePath}cof/{Name}.cof";
            var s = new FileStream(fileName, FileMode.Open);
            var reader = new BinaryReader(s);

            var palPath = basePath + "cof/palshift.dat";
            if(File.Exists(palPath))
            {
                Palette = new byte[256];
                var buffer = File.ReadAllBytes(palPath);
                Array.Copy(buffer, 256 * (PaletteIdx - 1), Palette, 0, Palette.Length);
            }

            // layers
            LayersCount = reader.ReadByte();

            // frames per direction
            FramesCount = reader.ReadByte();

            // directions
            DirectionCount = reader.ReadByte();

            // skip 25 unknown bytes
            reader.ReadBytes(25);

            // layers infos
            for (var i = 0; i < LayersCount; i++)
            {
                // composit index
                var armorIdx = reader.ReadByte();

                // shadows
                var shad_a = reader.ReadByte();
                var shad_b = reader.ReadByte();
                var transparency_a = reader.ReadByte();
                var transparency_b = reader.ReadByte();

                // weapon class (used to know a part of the dcc name)
                var wclass = TDiabloMap.ReadZString(reader);

                // dcc / dc6
                var armor = Armor[armorIdx];
                var layerType = LayerType[armorIdx];
                var dccName = $"{basePath}{layerType}/{Token}{layerType}{armor}{Mode}{wclass}.dcc";

                if (File.Exists(dccName))
                {
                    ReadDcc(new FileStream(dccName, FileMode.Open));
                }
                else
                {
                    dccName = dccName.Substring(0, dccName.Length - 1) + "6";
                    ReadDc6(new FileStream(dccName, FileMode.Open));
                }
            }

            // skip flags of each frames
            reader.ReadBytes(FramesCount);

            // priority layer
            priority = reader.ReadBytes(DirectionCount * FramesCount * LayersCount);

            // default animation speed
            //cof->spd_mul = 1;
            //cof->spd_div = 256;

            //// default x and y offsets
            //cof->xoffset = cof->yoffset = 0;

            //// speed info : try in animdata.d2
            //sprintf(animdata_name, "%s%s%s", tok, mod, clas);
            //if (animdata_get_cof_info(animdata_name, &animdata_fpd, &animdata_speed) == 0)
            //{
            //    // found
            //    //      cof->fpd     = animdata_fpd;
            //    cof->spd_mul = animdata_speed; // can be override by objects.txt values
            //    cof->spd_div = 256;
            //}

            //// objects.txt ID of that obj
            //sptr = txt->data +
            //       (obj_line * txt->line_size) +
            //       txt->col[glb_ds1edit.col_obj_id].offset;
            //lptr = (long*)sptr;
            //id = *lptr;
            //printf("object %s ID = %li\n", name, id);


            //// which mode is this obj ?
            //if (stricmp(mod, "NU") == 0)
            //    mode = 0;
            //else if (stricmp(mod, "OP") == 0)
            //    mode = 1;
            //else if (stricmp(mod, "ON") == 0)
            //    mode = 2;
            //else if (stricmp(mod, "S1") == 0)
            //    mode = 3;
            //else if (stricmp(mod, "S2") == 0)
            //    mode = 4;
            //else if (stricmp(mod, "S3") == 0)
            //    mode = 5;
            //else if (stricmp(mod, "S4") == 0)
            //    mode = 6;
            //else if (stricmp(mod, "S5") == 0)
            //    mode = 7;
            //else
            //{
            //    // invalid object's mode, or simply not an object COF (like a monster COF)
            //    // end
            //    free(buff);
            //    if (pal_buff)
            //        free(pal_buff);
            //    return cof;
            //}

            //// search line in objects.txt for this ID
            //if (id)
            //{
            //    done = FALSE;
            //    i = 0;
            //    line = 0;
            //    glb_ds1edit.obj_desc[obj_line].objects_line = -1;
            //    while (!done)
            //    {
            //        sptr = txt2->data +
            //               (i * txt2->line_size) +
            //               txt2->col[glb_ds1edit.col_objects_id].offset;
            //        lptr = (long*)sptr;
            //        if ((*lptr) == id)
            //        {
            //            done = TRUE;
            //            line = i;
            //        }
            //        else
            //        {
            //            i++;
            //            if (i >= txt2->line_num)
            //            {
            //                // end
            //                free(buff);
            //                if (pal_buff)
            //                    free(pal_buff);
            //                return cof;
            //            }
            //        }
            //    }
            //    glb_ds1edit.obj_desc[obj_line].objects_line = line;

            //    // speed multiplicator
            //    sptr =
            //       txt2->data +
            //       (line * txt2->line_size) +
            //       txt2->col[glb_ds1edit.col_frame_delta[mode]].offset;
            //    lptr = (long*)sptr;
            //    cof->spd_mul = (*lptr) == 0 ? 256 : (*lptr);

            //    // speed divisor
            //    cof->spd_div = 256;

            //    // xoffset & yoffset
            //    if (txt2->col[misc_get_txt_column_num(RQ_OBJECTS, "Xoffset")].size)
            //    {
            //        sptr = txt2->data + (line * txt2->line_size) +
            //               txt2->col[misc_get_txt_column_num(RQ_OBJECTS, "Xoffset")].offset;
            //        lptr = (long*)sptr;
            //        cof->xoffset = *lptr;
            //    }
            //    if (txt2->col[misc_get_txt_column_num(RQ_OBJECTS, "Yoffset")].size)
            //    {
            //        sptr = txt2->data + (line * txt2->line_size) +
            //               txt2->col[misc_get_txt_column_num(RQ_OBJECTS, "Yoffset")].offset;
            //        lptr = (long*)sptr;
            //        cof->yoffset = *lptr;
            //    }

            //    // orderflag
            //    if (txt2->col[glb_ds1edit.col_orderflag[mode]].size)
            //    {
            //        sptr =
            //           txt2->data +
            //           (line * txt2->line_size) +
            //           txt2->col[glb_ds1edit.col_orderflag[mode]].offset;
            //        lptr = (long*)sptr;
            //        cof->orderflag = *lptr;

            //        // if 0, check NU
            //        // because Mephisto bridge only have a 1 in the NU mode
            //        if (*lptr == 0)
            //        {
            //            if (txt2->col[glb_ds1edit.col_orderflag[0]].size)
            //            {
            //                sptr =
            //                   txt2->data +
            //                   (line * txt2->line_size) +
            //                   txt2->col[glb_ds1edit.col_orderflag[0]].offset;
            //                lptr = (long*)sptr;
            //                cof->orderflag = *lptr;
            //            }
            //        }

            //        printf("object %s orderflag = %li\n", name, cof->orderflag);
            //    }
            //}
        
    }
    }
}
