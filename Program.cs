/*
 * The MIT License
 *
 * Copyright 2015 Kiyofumi Kondoh
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

using System;
using System.Collections.Generic;
using System.Text;

namespace test_enum_wpd_cs
{
    class Program
    {
        const string szClientName = "WPD client";
        const uint dwClientVersionMajor = 1;
        const uint dwClientVersionMinor = 0;
        const uint dwClientRevision = 1;

        static uint MY_FETCH_COUNT = 1;
        static uint countContent = 0;

        static bool wpdEnumContent(string strObjectId, PortableDeviceApiLib.IPortableDeviceContent content)
        {
            if (null == strObjectId)
            {
                return false;
            }
            if (null == content)
            {
                return false;
            }

            //Console.WriteLine(strObjectId);

            bool result = true;

            PortableDeviceApiLib.IEnumPortableDeviceObjectIDs enumObj = null;

            const uint dwFlags = 0;
            const PortableDeviceApiLib.IPortableDeviceValues filter = null;
            try
            {
                content.EnumObjects(dwFlags, strObjectId, filter, out enumObj);
            }
            catch (System.Exception e)
            {
                if (e is System.Runtime.InteropServices.COMException)
                {
                    System.Runtime.InteropServices.COMException ex =
                        (System.Runtime.InteropServices.COMException)e;
                    Console.WriteLine("EnumObjects, hr={0:X}", (uint)ex.ErrorCode);
                }
                else
                {
                    Console.WriteLine(e.Message);
                }
                //Console.WriteLine(e.StackTrace);
                return false;
            }

            if (null == enumObj)
            {
                return false;
            }

            string[] strObjectIdArray = new string[MY_FETCH_COUNT];
            //string strObjectIdArray = "";
            uint nFetched = 0;
            do
            {
                // MY_FETCH_COUNT 1 is allowed. over 2, null reference occured and leak CoTaskMemFree at marshall
                System.Diagnostics.Debug.Assert(1 == MY_FETCH_COUNT);
                try
                {
                    enumObj.Next(MY_FETCH_COUNT, out strObjectIdArray[0], ref nFetched);
                }
                catch (System.Exception e)
                {
                    if (e is System.Runtime.InteropServices.COMException)
                    {
                        System.Runtime.InteropServices.COMException ex =
                            (System.Runtime.InteropServices.COMException)e;
                        Console.WriteLine("enumObj Next, hr={0:X}", (uint)ex.ErrorCode);
                    }
                    else
                    {
                        Console.WriteLine(e.Message);
                    }
                    //Console.WriteLine(e.StackTrace);
                    result = false;
                    nFetched = 0;
                    break;
                }

                if (0 < nFetched)
                {
                    countContent += nFetched;
                    for (uint index = 0; index < nFetched; ++index)
                    {
                        result = wpdEnumContent(strObjectIdArray[index], content);
                        if (false == result)
                        {
                            break;
                        }
                    }

                    if (false == result)
                    {
                        nFetched = 0;
                        break;
                    }
                }
            } while (0 < nFetched);

            enumObj = null;

            return result;
        }

        static void Main(string[] args)
        {
            PortableDeviceApiLib.IPortableDeviceValues clientInfo =
                (PortableDeviceApiLib.IPortableDeviceValues)new PortableDeviceTypesLib.PortableDeviceValues();

            {
                clientInfo.SetStringValue(ref WPD_CLIENT_NAME, szClientName);
                clientInfo.SetUnsignedIntegerValue(ref WPD_CLIENT_MAJOR_VERSION, dwClientVersionMajor);
                clientInfo.SetUnsignedIntegerValue(ref WPD_CLIENT_MINOR_VERSION, dwClientVersionMinor);
                clientInfo.SetUnsignedIntegerValue(ref WPD_CLIENT_REVISION, dwClientRevision);
                clientInfo.SetUnsignedIntegerValue(ref WPD_CLIENT_SECURITY_QUALITY_OF_SERVICE, SECURITY_IMPERSONATION);
                clientInfo.SetUnsignedIntegerValue(ref WPD_CLIENT_DESIRED_ACCESS, GENERIC_READ);
                clientInfo.SetUnsignedIntegerValue(ref WPD_CLIENT_SHARE_MODE, FILE_SHARE_READ | FILE_SHARE_WRITE);
            }


            for (uint loopCount = 0; loopCount < 100; ++loopCount)
            {
                PortableDeviceApiLib.PortableDeviceManager devMgr =
                    new PortableDeviceApiLib.PortableDeviceManager();

                devMgr.RefreshDeviceList();

                string strDeviceId = null;
                uint nDeviceCount = 1;  // 1 is allowed. over 2, leak CoTaskMemFree at marshall

                System.Diagnostics.Debug.Assert(1 == nDeviceCount);
                devMgr.GetDevices(ref strDeviceId, ref nDeviceCount);
                System.Console.WriteLine("DeviceCount={0}", nDeviceCount);
                if (0 < nDeviceCount)
                {
                    //Console.WriteLine(strDeviceId);
                    PortableDeviceApiLib.PortableDevice dev =
                        new PortableDeviceApiLib.PortableDevice();

                    dev.Open(strDeviceId, clientInfo);

                    PortableDeviceApiLib.IPortableDeviceContent content = null;
                    dev.Content(out content);
                    if (null != content)
                    {
                        for (uint count = 0; count < 30; ++count)
                        {
                            Console.WriteLine("enumrate start");
                            countContent = 0;
                            bool result = wpdEnumContent(WPD_DEVICE_OBJECT_ID, content);
                            Console.WriteLine("enumrate end: count count={0}", countContent);
                            if (false == result)
                            {
                                break;
                            }
                            System.Threading.Thread.Sleep(1 * 1000);
                        }
                        content = null;
                    }

                    dev.Close();
                    dev = null;
                }

                devMgr = null;
                System.Threading.Thread.Sleep(1 * 1000);
            }
        }

        const uint SECURITY_ANONYMOUS       = 0x00000000;
        const uint SECURITY_IDENTIFICATION  = 0x00010000;
        const uint SECURITY_IMPERSONATION   = 0x00020000;
        const uint SECURITY_DELEGATION      = 0x00030000;

        const uint GENERIC_READ     = 0x80000000;
        const uint GENERIC_WRITE    = 0x40000000;
        const uint GENERIC_EXECUTE  = 0x20000000;
        const uint GENERIC_ALL      = 0x10000000;

        const uint FILE_SHARE_READ      = 0x00000001;
        const uint FILE_SHARE_WRITE     = 0x00000002;
        const uint FILE_SHARE_DELETE    = 0x00000004;


        const string WPD_DEVICE_OBJECT_ID = "DEVICE";

        static PortableDeviceApiLib._tagpropertykey WPD_OBJECT_ID =
            genTagPropertyKey(0xEF6B490D, 0x5CD8, 0x437A, 0xAF, 0xFC, 0xDA, 0x8B, 0x60, 0xEE, 0x4A, 0x3C, 2);
        static PortableDeviceApiLib._tagpropertykey WPD_OBJECT_PARENT_ID =
            genTagPropertyKey(0xEF6B490D, 0x5CD8, 0x437A, 0xAF, 0xFC, 0xDA, 0x8B, 0x60, 0xEE, 0x4A, 0x3C, 3);
        static PortableDeviceApiLib._tagpropertykey WPD_OBJECT_NAME =
            genTagPropertyKey(0xEF6B490D, 0x5CD8, 0x437A, 0xAF, 0xFC, 0xDA, 0x8B, 0x60, 0xEE, 0x4A, 0x3C, 4);
        static PortableDeviceApiLib._tagpropertykey WPD_OBJECT_PERSISTENT_UNIQUE_ID =
            genTagPropertyKey(0xEF6B490D, 0x5CD8, 0x437A, 0xAF, 0xFC, 0xDA, 0x8B, 0x60, 0xEE, 0x4A, 0x3C, 5);
        static PortableDeviceApiLib._tagpropertykey WPD_OBJECT_FORMAT =
            genTagPropertyKey(0xEF6B490D, 0x5CD8, 0x437A, 0xAF, 0xFC, 0xDA, 0x8B, 0x60, 0xEE, 0x4A, 0x3C, 6);
        static PortableDeviceApiLib._tagpropertykey WPD_OBJECT_SIZE =
            genTagPropertyKey(0xEF6B490D, 0x5CD8, 0x437A, 0xAF, 0xFC, 0xDA, 0x8B, 0x60, 0xEE, 0x4A, 0x3C, 11);
        static PortableDeviceApiLib._tagpropertykey WPD_OBJECT_ORIGINAL_FILE_NAME =
            genTagPropertyKey(0xEF6B490D, 0x5CD8, 0x437A, 0xAF, 0xFC, 0xDA, 0x8B, 0x60, 0xEE, 0x4A, 0x3C, 12);
        static PortableDeviceApiLib._tagpropertykey WPD_OBJECT_DATE_CREATED =
            genTagPropertyKey(0xEF6B490D, 0x5CD8, 0x437A, 0xAF, 0xFC, 0xDA, 0x8B, 0x60, 0xEE, 0x4A, 0x3C, 18);
        static PortableDeviceApiLib._tagpropertykey WPD_OBJECT_DATE_MODIFIED =
            genTagPropertyKey(0xEF6B490D, 0x5CD8, 0x437A, 0xAF, 0xFC, 0xDA, 0x8B, 0x60, 0xEE, 0x4A, 0x3C, 19);


        static PortableDeviceApiLib._tagpropertykey WPD_CLIENT_NAME =
            genTagPropertyKey(0x204D9F0C, 0x2292, 0x4080, 0x9F, 0x42, 0x40, 0x66, 0x4E, 0x70, 0xF8, 0x59, 2);
        static PortableDeviceApiLib._tagpropertykey WPD_CLIENT_MAJOR_VERSION =
            genTagPropertyKey(0x204D9F0C, 0x2292, 0x4080, 0x9F, 0x42, 0x40, 0x66, 0x4E, 0x70, 0xF8, 0x59, 3);
        static PortableDeviceApiLib._tagpropertykey WPD_CLIENT_MINOR_VERSION =
            genTagPropertyKey(0x204D9F0C, 0x2292, 0x4080, 0x9F, 0x42, 0x40, 0x66, 0x4E, 0x70, 0xF8, 0x59, 4);
        static PortableDeviceApiLib._tagpropertykey WPD_CLIENT_REVISION =
            genTagPropertyKey(0x204D9F0C, 0x2292, 0x4080, 0x9F, 0x42, 0x40, 0x66, 0x4E, 0x70, 0xF8, 0x59, 5);
        static PortableDeviceApiLib._tagpropertykey WPD_CLIENT_SECURITY_QUALITY_OF_SERVICE =
            genTagPropertyKey(0x204D9F0C, 0x2292, 0x4080, 0x9F, 0x42, 0x40, 0x66, 0x4E, 0x70, 0xF8, 0x59, 8);
        static PortableDeviceApiLib._tagpropertykey WPD_CLIENT_DESIRED_ACCESS =
            genTagPropertyKey(0x204D9F0C, 0x2292, 0x4080, 0x9F, 0x42, 0x40, 0x66, 0x4E, 0x70, 0xF8, 0x59, 9);
        static PortableDeviceApiLib._tagpropertykey WPD_CLIENT_SHARE_MODE =
            genTagPropertyKey(0x204D9F0C, 0x2292, 0x4080, 0x9F, 0x42, 0x40, 0x66, 0x4E, 0x70, 0xF8, 0x59, 10);
        static PortableDeviceApiLib._tagpropertykey WPD_CLIENT_EVENT_COOKIE =
            genTagPropertyKey(0x204D9F0C, 0x2292, 0x4080, 0x9F, 0x42, 0x40, 0x66, 0x4E, 0x70, 0xF8, 0x59, 11);

        static PortableDeviceApiLib._tagpropertykey
            genTagPropertyKey(uint a, ushort b, ushort c, byte d, byte e, byte f, byte g, byte h, byte i, byte j, byte k, uint pid )
        {
            PortableDeviceApiLib._tagpropertykey value =
                new PortableDeviceApiLib._tagpropertykey();

            value.fmtid = new Guid(a, b, c, d, e, f, g, h, i, j, k);
            value.pid = pid;

            return value;
        }
    }

}
