using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Diablo2RProject
{
    public enum Version : Int32
    {
        versionEncodesFiles = 3,
        versionEncodesFloorLayers = 4,
        versionSimpleLayerHigh = 4,
        versionEncodesWallLayers = 16,
        versionEncodesAct = 8,
        versionEncodesSubtitutionLayers = 10,
        versionEncodesSubtitutionGroups = 12,
        versionEncodesNpcs = 14,
        versionEncodesNpcExtraData = 15,
        versionHasUnknownBytes2 = 18,
        versionUnknowBytes1Low = 9,
        versionUnknowBytes1High = 13
    }

    public static class VersionExtensions
    {
        public static bool EncodeAct(this Version version)
        {
            return version >= Version.versionEncodesAct;
        }

        public static bool EncodeFiles(this Version version)
        {
            return version >= Version.versionEncodesFiles;
        }

        public static bool EncodeFloorLayers(this Version version)
        {
            return version >= Version.versionEncodesFloorLayers;
        }

        public static bool EncodeWallLayers(this Version version)
        {
            return version >= Version.versionEncodesWallLayers;
        }

        public static bool HasUnknownBytes1(this Version version)
        {
            return version >= Version.versionUnknowBytes1Low &&
                   version <= Version.versionUnknowBytes1High;
        }

        public static bool HasUnknownBytes2(this Version version)
        {
            return version >= Version.versionHasUnknownBytes2;
        }

        public static bool EncodeSubstitutionLayers(this Version version)
        {
            return version >= Version.versionEncodesSubtitutionLayers;
        }

        public static bool EncodeSubstitutionGroups(this Version version)
        {
            return version >= Version.versionEncodesSubtitutionGroups;
        }

        public static bool EncodeNPCs(this Version version)
        {
            return version >= Version.versionEncodesNpcs;
        }

        public static bool EncodeNPCExtraData(this  Version version)
        {
            return version >= Version.versionEncodesNpcExtraData;
        }

        public static bool EncodeSimpleLayers(this Version version)
        {
            return version >= Version.versionSimpleLayerHigh;
        }
    }
}
