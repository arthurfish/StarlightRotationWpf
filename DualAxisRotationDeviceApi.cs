using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Leadshine.SMC;
using Leadshine.SMC.IDE.Motion;


namespace StarlightRotationWpf
{
    public class DualAxisRotationDeviceApi
    {
        public bool isConnected { get; }
        private ushort horizentalAxis = 1;
        private ushort verticalAxis = 0;
        private double verticalEquiv = 10000 / 360.0 * 880;
        private double horizentalEquiv = 12800 / 360.0 * 180;

        public DualAxisRotationDeviceApi(string ipAddress = "192.168.5.11")
        {
            short iret = LTSMC.smc_board_init(0, 2, ipAddress, 0);
            if (iret != 0)
            {
                Trace.WriteLine("DualAxisDevice: CAN NOT CONNECT!!!!");
                isConnected = false;
            }
            isConnected = true;

            LTSMC.smc_set_equiv(0, horizentalAxis, horizentalEquiv);
            LTSMC.smc_set_equiv(0, verticalAxis, verticalEquiv);

            LTSMC.smc_set_profile_unit(0, horizentalAxis, 0, 10, 0, 0, 0);
            LTSMC.smc_set_profile_unit(0, verticalAxis, 0, 10, 0, 0, 0);
        }

        ~DualAxisRotationDeviceApi()
        {
            LTSMC.smc_board_close(0);
        }

        public void setHorizentalRotationInDegree(double degree)
        {
            if (!isConnected)
                return;
            LTSMC.smc_pmove_unit(0, horizentalAxis, degree % 360, 1);
        }

        public void setVerticalRotationInDegree(double degree)
        {
            if (!isConnected)
                return;
            LTSMC.smc_pmove_unit(0, verticalAxis, degree % 360, 1);
        }

        public double getHorizentalRotationInDegree()
        {
            if (!isConnected)
                return 0;
            double pos = 0;
            LTSMC.smc_get_position_unit(0, horizentalAxis, ref pos);
            return pos;
        }

        public double getVerticalRotationInDegree()
        {
            if (!isConnected)
                return 0;
            double pos = 0;
            LTSMC.smc_get_position_unit(0, verticalAxis, ref pos);
            return pos;
        }

        public void emergencyStop()
        {
            if (!isConnected)
                return;
            LTSMC.smc_stop(0, verticalAxis, 0);
            LTSMC.smc_stop(0, horizentalAxis, 0);
        }

        public bool isAvaliable()
        {
            return LTSMC.smc_check_done(0, horizentalAxis) == 1 && LTSMC.smc_check_done(0, verticalAxis) == 1;
        }
    }
}
