using HalconDotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaslerCamera
{
    public class Tool
    {


        //坐标系转换
        public static void xyz2zyx(HTuple hv_r1, HTuple hv_r2, HTuple hv_r3, out HTuple hv_R1_new,
    out HTuple hv_R2_new, out HTuple hv_R3_new)
        {



            // Local iconic variables 

            // Local control variables 

            HTuple hv_Pose = new HTuple(), hv_PoseOut = new HTuple();
            // Initialize local and output iconic variables 
            hv_R1_new = new HTuple();
            hv_R2_new = new HTuple();
            hv_R3_new = new HTuple();
            hv_Pose.Dispose();
            HOperatorSet.CreatePose(0, 0, 0, hv_r1, hv_r2, hv_r3, "Rp+T", "abg", "point",
                out hv_Pose);
            hv_PoseOut.Dispose();
            HOperatorSet.ConvertPoseType(hv_Pose, "Rp+T", "gba", "point", out hv_PoseOut);
            hv_R1_new.Dispose();
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                hv_R1_new = hv_PoseOut.TupleSelect(
                    3);
            }
            hv_R2_new.Dispose();
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                hv_R2_new = hv_PoseOut.TupleSelect(
                    4);
            }
            hv_R3_new.Dispose();
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                hv_R3_new = hv_PoseOut.TupleSelect(
                    5);
            }

            hv_Pose.Dispose();
            hv_PoseOut.Dispose();

            return;
        }

        public static void xyz2zyz(HTuple hv_r1, HTuple hv_r2, HTuple hv_r3, out HTuple hv_R1_new,
            out HTuple hv_R2_new, out HTuple hv_R3_new)
        {



            // Local iconic variables 

            // Local control variables 

            HTuple hv_Pose = new HTuple(), hv_HomMat3DXYZ = new HTuple();
            // Initialize local and output iconic variables 
            hv_R1_new = new HTuple();
            hv_R2_new = new HTuple();
            hv_R3_new = new HTuple();
            hv_Pose.Dispose();
            HOperatorSet.CreatePose(0, 0, 0, hv_r1, hv_r2, hv_r3, "Rp+T", "abg", "point",
                out hv_Pose);
            hv_HomMat3DXYZ.Dispose();
            HOperatorSet.PoseToHomMat3d(hv_Pose, out hv_HomMat3DXYZ);
            //ZYZ
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                hv_R1_new.Dispose();
                HOperatorSet.TupleAtan2(hv_HomMat3DXYZ.TupleSelect(6), hv_HomMat3DXYZ.TupleSelect(
                    2), out hv_R1_new);
            }
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                hv_R2_new.Dispose();
                HOperatorSet.TupleAtan2(((1 - (((hv_HomMat3DXYZ.TupleSelect(10))).TuplePow(2)))).TupleSqrt()
                    , hv_HomMat3DXYZ.TupleSelect(10), out hv_R2_new);
            }
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                hv_R3_new.Dispose();
                HOperatorSet.TupleAtan2(hv_HomMat3DXYZ.TupleSelect(9), -(hv_HomMat3DXYZ.TupleSelect(
                    8)), out hv_R3_new);
            }
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                {
                    HTuple
                      ExpTmpLocalVar_R1_new = hv_R1_new.TupleDeg()
                        ;
                    hv_R1_new.Dispose();
                    hv_R1_new = ExpTmpLocalVar_R1_new;
                }
            }
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                {
                    HTuple
                      ExpTmpLocalVar_R2_new = hv_R2_new.TupleDeg()
                        ;
                    hv_R2_new.Dispose();
                    hv_R2_new = ExpTmpLocalVar_R2_new;
                }
            }
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                {
                    HTuple
                      ExpTmpLocalVar_R3_new = hv_R3_new.TupleDeg()
                        ;
                    hv_R3_new.Dispose();
                    hv_R3_new = ExpTmpLocalVar_R3_new;
                }
            }

            hv_Pose.Dispose();
            hv_HomMat3DXYZ.Dispose();

            return;
        }

        public static void zyx2xyz(HTuple hv_r1, HTuple hv_r2, HTuple hv_r3, out HTuple hv_R1_new,
            out HTuple hv_R2_new, out HTuple hv_R3_new)
        {



            // Local iconic variables 

            // Local control variables 

            HTuple hv_Pose = new HTuple(), hv_PoseOut = new HTuple();
            // Initialize local and output iconic variables 
            hv_R1_new = new HTuple();
            hv_R2_new = new HTuple();
            hv_R3_new = new HTuple();
            hv_Pose.Dispose();
            HOperatorSet.CreatePose(0, 0, 0, hv_r1, hv_r2, hv_r3, "Rp+T", "gba", "point",
                out hv_Pose);
            hv_PoseOut.Dispose();
            HOperatorSet.ConvertPoseType(hv_Pose, "Rp+T", "abg", "point", out hv_PoseOut);
            hv_R1_new.Dispose();
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                hv_R1_new = hv_PoseOut.TupleSelect(
                    3);
            }
            hv_R2_new.Dispose();
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                hv_R2_new = hv_PoseOut.TupleSelect(
                    4);
            }
            hv_R3_new.Dispose();
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                hv_R3_new = hv_PoseOut.TupleSelect(
                    5);
            }

            hv_Pose.Dispose();
            hv_PoseOut.Dispose();

            return;
        }

        public static void zyx2zyz(HTuple hv_r1, HTuple hv_r2, HTuple hv_r3, out HTuple hv_R1_new,
            out HTuple hv_R2_new, out HTuple hv_R3_new)
        {


            // Initialize local and output iconic variables 
            hv_R1_new = new HTuple();
            hv_R2_new = new HTuple();
            hv_R3_new = new HTuple();
            hv_R1_new.Dispose(); hv_R2_new.Dispose(); hv_R3_new.Dispose();
            zyx2xyz(hv_r1, hv_r2, hv_r3, out hv_R1_new, out hv_R2_new, out hv_R3_new);
            {
                HTuple ExpTmpOutVar_0; HTuple ExpTmpOutVar_1; HTuple ExpTmpOutVar_2;
                xyz2zyz(hv_R1_new, hv_R2_new, hv_R3_new, out ExpTmpOutVar_0, out ExpTmpOutVar_1,
                    out ExpTmpOutVar_2);
                hv_R1_new.Dispose();
                hv_R1_new = ExpTmpOutVar_0;
                hv_R2_new.Dispose();
                hv_R2_new = ExpTmpOutVar_1;
                hv_R3_new.Dispose();
                hv_R3_new = ExpTmpOutVar_2;
            }


            return;
        }

        public static void zyz2xyz(HTuple hv_r1, HTuple hv_r2, HTuple hv_r3, out HTuple hv_R1_new,
            out HTuple hv_R2_new, out HTuple hv_R3_new)
        {



            // Local iconic variables 

            // Local control variables 

            HTuple hv_HomMat3DIdentity = new HTuple();
            HTuple hv_HomMat3DRotate = new HTuple(), hv_HomMat3DRotate1 = new HTuple();
            HTuple hv_HomMat3DRotate2 = new HTuple(), hv_Pose5 = new HTuple();
            HTuple hv_PoseXYZ = new HTuple();
            // Initialize local and output iconic variables 
            hv_R1_new = new HTuple();
            hv_R2_new = new HTuple();
            hv_R3_new = new HTuple();
            hv_HomMat3DIdentity.Dispose();
            HOperatorSet.HomMat3dIdentity(out hv_HomMat3DIdentity);
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                hv_HomMat3DRotate.Dispose();
                HOperatorSet.HomMat3dRotateLocal(hv_HomMat3DIdentity, hv_r1.TupleRad(), "z",
                    out hv_HomMat3DRotate);
            }
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                hv_HomMat3DRotate1.Dispose();
                HOperatorSet.HomMat3dRotateLocal(hv_HomMat3DRotate, hv_r2.TupleRad(), "y", out hv_HomMat3DRotate1);
            }
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                hv_HomMat3DRotate2.Dispose();
                HOperatorSet.HomMat3dRotateLocal(hv_HomMat3DRotate1, hv_r3.TupleRad(), "z", out hv_HomMat3DRotate2);
            }
            hv_Pose5.Dispose();
            HOperatorSet.HomMat3dToPose(hv_HomMat3DRotate2, out hv_Pose5);
            hv_PoseXYZ.Dispose();
            HOperatorSet.ConvertPoseType(hv_Pose5, "Rp+T", "abg", "point", out hv_PoseXYZ);

            hv_R1_new.Dispose();
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                hv_R1_new = hv_PoseXYZ.TupleSelect(
                    3);
            }
            hv_R2_new.Dispose();
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                hv_R2_new = hv_PoseXYZ.TupleSelect(
                    4);
            }
            hv_R3_new.Dispose();
            using (HDevDisposeHelper dh = new HDevDisposeHelper())
            {
                hv_R3_new = hv_PoseXYZ.TupleSelect(
                    5);
            }

            hv_HomMat3DIdentity.Dispose();
            hv_HomMat3DRotate.Dispose();
            hv_HomMat3DRotate1.Dispose();
            hv_HomMat3DRotate2.Dispose();
            hv_Pose5.Dispose();
            hv_PoseXYZ.Dispose();

            return;
        }

        public static void zyz2zyx(HTuple hv_r1, HTuple hv_r2, HTuple hv_r3, out HTuple hv_R1_new,
            out HTuple hv_R2_new, out HTuple hv_R3_new)
        {


            // Initialize local and output iconic variables 
            hv_R1_new = new HTuple();
            hv_R2_new = new HTuple();
            hv_R3_new = new HTuple();
            hv_R1_new.Dispose(); hv_R2_new.Dispose(); hv_R3_new.Dispose();
            zyz2xyz(hv_r1, hv_r2, hv_r3, out hv_R1_new, out hv_R2_new, out hv_R3_new);
            {
                HTuple ExpTmpOutVar_0; HTuple ExpTmpOutVar_1; HTuple ExpTmpOutVar_2;
                xyz2zyx(hv_R1_new, hv_R2_new, hv_R3_new, out ExpTmpOutVar_0, out ExpTmpOutVar_1,
                    out ExpTmpOutVar_2);
                hv_R1_new.Dispose();
                hv_R1_new = ExpTmpOutVar_0;
                hv_R2_new.Dispose();
                hv_R2_new = ExpTmpOutVar_1;
                hv_R3_new.Dispose();
                hv_R3_new = ExpTmpOutVar_2;
            }


            return;
        }

        ///PoseType 0是xyz，1是zyx，2是zyz,这里是用halcon实现
        public static int transformCartPose2(double RX, double RY, double RZ, int originCarPoseType,
              ref double transformRX, ref double transformRY, ref double transformRZ, int dstCarPoseType)
        {
            if (originCarPoseType == 0)
            {
                if (dstCarPoseType == 0)
                {
                    transformRX = RX; transformRY = RY; transformRZ = RZ;
                }
                else if (dstCarPoseType == 1)
                {
                    xyz2zyx(RX, RY, RZ, out HTuple hvR1, out HTuple hvR2, out HTuple hvR3);

                    transformRX = hvR1.D;
                    transformRY = hvR2.D;
                    transformRZ = hvR3.D;

                }
                else
                {
                    xyz2zyz(RX, RY, RZ, out HTuple hvR1, out HTuple hvR2, out HTuple hvR3);

                    transformRX = hvR1.D;
                    transformRY = hvR2.D;
                    transformRZ = hvR3.D;
                }
            }
            else if (originCarPoseType == 1)
            {
                if (dstCarPoseType == 0)
                {
                    zyx2xyz(RX, RY, RZ, out HTuple hvR1, out HTuple hvR2, out HTuple hvR3);

                    transformRX = hvR1.D;
                    transformRY = hvR2.D;
                    transformRZ = hvR3.D;
                }
                else if (dstCarPoseType == 1)
                {
                    transformRX = RX; transformRY = RY; transformRZ = RZ;

                }
                else
                {
                    zyx2zyz(RX, RY, RZ, out HTuple hvR1, out HTuple hvR2, out HTuple hvR3);

                    transformRX = hvR1.D;
                    transformRY = hvR2.D;
                    transformRZ = hvR3.D;
                }
            }
            else
            {
                if (dstCarPoseType == 0)
                {
                    zyz2xyz(RX, RY, RZ, out HTuple hvR1, out HTuple hvR2, out HTuple hvR3);

                    transformRX = hvR1.D;
                    transformRY = hvR2.D;
                    transformRZ = hvR3.D;
                }
                else if (dstCarPoseType == 1)
                {
                    zyz2zyx(RX, RY, RZ, out HTuple hvR1, out HTuple hvR2, out HTuple hvR3);

                    transformRX = hvR1.D;
                    transformRY = hvR2.D;
                    transformRZ = hvR3.D;
                }
                else
                {
                    transformRX = RX; transformRY = RY; transformRZ = RZ;

                }
            }


            return 0;
        }

    }
}
