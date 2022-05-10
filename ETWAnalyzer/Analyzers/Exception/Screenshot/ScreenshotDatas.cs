//// SPDX-FileCopyrightText:  © 2022 Siemens Healthcare GmbH
//// SPDX-License-Identifier:   MIT

using ETWAnalyzer.Extract;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace ETWAnalyzer.Analyzers
{
    /// <summary>
    /// Linking the Bitmaps to the TestExtractData - Object 
    /// </summary>
    public class TestDataWithBitMap
    {
        /// <summary>
        /// First taken screenshot
        /// </summary>
        public Bitmap FirstScreenshot { get; private set; }

        /// <summary>
        /// The Second screenshot is recorded after a time delay (more significant)
        /// </summary>
        public Bitmap SecondScreenshot { get; private set; }

        /// <summary>
        /// To allocate and analyze the screenshots
        /// </summary>
        public TestDataFile BelongsToTestExtractData { get; private set; }


        /// <summary>
        /// Generates an Instance and sets the expected color - values of the Test
        /// </summary>
        /// <param name="firstScreenShot">Bitmap of the first recorded screenshot</param>
        /// <param name="secondScreenshot">Bitmap of the second recorded screenshot (timedelay)</param>
        /// <param name="testdata">All relevant file data - testExtractData belongs to the screenshots</param>
        public TestDataWithBitMap(Bitmap firstScreenShot, Bitmap secondScreenshot, TestDataFile testdata)
        {
            FirstScreenshot = firstScreenShot;
            SecondScreenshot = secondScreenshot;
            BelongsToTestExtractData = testdata;
        }
    }





    ///// <summary>
    ///// 
    ///// </summary>
    //public class TestWithExpectedColors
    //{
    //    /// <summary>
    //    /// 
    //    /// </summary>
    //    public string TestName { get; private set; }

    //    /// <summary>
    //    /// 
    //    /// </summary>
    //    public string BitmappingPath { get; private set; }

    //    /// <summary>
    //    /// Multiple versions of sample snapshots are used.
    //    /// Contains an instance of every version which is stored in the configfolder
    //    /// </summary>
    //    public List<ScreenshotData> ExpectedColors { get; } = new List<ScreenshotData>();

    //    /// <summary>
    //    /// Generates an instance by calling SetExpectedColors method
    //    /// </summary>
    //    /// <param name="pathOfBitmaps">path where snapshots stored</param>
    //    public TestWithExpectedColors(string pathOfBitmaps)
    //    {
    //        BitmappingPath = pathOfBitmaps ?? throw new ArgumentNullException(nameof(pathOfBitmaps));
    //        TestName = new DirectoryInfo(pathOfBitmaps).Name;
    //        SetExpectedColors();
    //    }
    //    /// <summary>
    //    /// Sets the expected colors form the config by generating ScreenshotData instances
    //    /// </summary>
    //    private void SetExpectedColors()
    //    {
    //        string[] snapshots = Directory.GetFiles(BitmappingPath);
    //        foreach (var snapshot in snapshots)
    //        {
    //            ExpectedColors.Add(new ScreenshotData(new TestDataWithBitMap(new Bitmap(snapshot), null, new TestDataFile(TestName, Path.GetFileName(snapshot), new DateTime(), 0, 0, "", ""))));
    //        }
    //    }
    //}


    /// <summary>
    /// Contains the screenshot data of the test
    /// </summary>
    public class ScreenshotData
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        public ScreenshotData() { }

        /// <summary>
        /// Generates Colors instance
        /// Using testExtractWithBitMap.BelongsToTestExtractData.Origin.FileName to set the BelongsTo property
        /// Calling ReadBitmapping Method to set all other property values
        /// </summary>
        /// <param name="testExtractWithBitMap"></param>
        public ScreenshotData(TestDataWithBitMap testExtractWithBitMap)
        {
            if (testExtractWithBitMap is null)
            {
                throw new ArgumentNullException(nameof(testExtractWithBitMap));
            }

            BelongsTo = testExtractWithBitMap.BelongsToTestExtractData.FileName;
            if (!testExtractWithBitMap.BelongsToTestExtractData.FileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase) && testExtractWithBitMap.BelongsToTestExtractData.Screenshots[0] != null && testExtractWithBitMap.BelongsToTestExtractData.Screenshots[1] != null)
            {
                TryToSetScreenshots(testExtractWithBitMap,testExtractWithBitMap.BelongsToTestExtractData.Screenshots[0],testExtractWithBitMap.BelongsToTestExtractData.Screenshots[1]);
            }
            if (testExtractWithBitMap.BelongsToTestExtractData.FileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            {
                TryToSetScreenshots(testExtractWithBitMap, testExtractWithBitMap.BelongsToTestExtractData.FileName, testExtractWithBitMap.BelongsToTestExtractData.FileName);
            }
        }

        /// <summary>
        /// First screenshot of the test
        /// </summary>
        public Screenshot FirstShot { get; set; }

        /// <summary>
        /// Second screenshot of the test
        /// </summary>
        public Screenshot SecondShot { get; set; }

        /// <summary>
        /// Test which the screenshots belong to
        /// </summary>
        public string BelongsTo { get; private set; }

        /// <summary>
        /// Sets the colors of the object for comparing by iterating over the 2D screenshots
        /// </summary>
        void TryToSetScreenshots(TestDataWithBitMap testDataWithBitMap,string firstPath,string secondPath)
        {
            if (testDataWithBitMap.FirstScreenshot != null)
            {
                FirstShot = new Screenshot(testDataWithBitMap.FirstScreenshot,firstPath);
            }
            if (testDataWithBitMap.SecondScreenshot != null)
            {
                SecondShot = new Screenshot(testDataWithBitMap.SecondScreenshot,secondPath);
            }
            if (Path.GetExtension(BelongsTo) != ".png")
            {
                if (FirstShot.Pixels == 0 && SecondShot.Pixels == 0)//no adding of Analysis Results here - already adding at another location
                {
                    Console.WriteLine($"Snapshots of {BelongsTo} missing.");
                }
                else
                {
                    if (testDataWithBitMap.FirstScreenshot == null || testDataWithBitMap.SecondScreenshot == null) Console.WriteLine($"One Snapshot of {BelongsTo} missing.");
                }
            }
        }

    }

    /// <summary>
    /// Describes the average colors of a screenshot
    /// </summary>
    public class Screenshot
    {
        /// <summary>
        /// Red average color per pixel
        /// </summary>
        public int RedComponentPerPixel { get; set; }
        /// <summary>
        /// Total red color component of the screenshot
        /// </summary>
        public int RedScreenshotComponent { get; set; }

        /// <summary>
        /// Green average color per pixel
        /// </summary>
        public int GreenComponentPerPixel { get; set; }
        /// <summary>
        /// Total green color component of the screenshot
        /// </summary>
        public int GreenScreenshotComponent { get; set; }
        /// <summary>
        /// Blue average color per pixel
        /// </summary>
        public int BlueComponentPerPixel { get; set; }
        /// <summary>
        /// Total blue color component of the screenshot
        /// </summary>
        public int BlueScreenshotComponent { get; set; }


        /// <summary>
        /// Retruns the sum of the total blue, green and red color components of the screenshot
        /// </summary>
        public int SumOfBlGrReComponents { get; set; }
        /// <summary>
        /// Returns the sum of the blue, green and red average color per pixel
        /// </summary>
        public int SumOfBlGrReComponentsPerPixel { get; set; }

        private int myPixels;
        /// <summary>
        /// Sets the number of Pixels
        /// Retruns the number of Pixels
        /// </summary>
        public int Pixels
        {
            set { myPixels = value; }
            get
            {
                if (Path.GetExtension(ScreenshotPath) == ".png" && myPixels == 0)
                {
                    using Bitmap b = new(ScreenshotPath);
                    myPixels = b.Width * b.Height;
                }
                return myPixels;
            }
        }

        /// <summary>
        /// Path of the Screenshot
        /// </summary>
        public string ScreenshotPath { get; set; } = "ScreenshotNotExists";

        /// <summary>
        /// Json Constructor
        /// </summary>
        public Screenshot()
        {
        }
        /// <summary>
        /// Generates a screenshotinstance for the given path
        /// </summary>
        /// <param name="path">screenshot path</param>
        public Screenshot(string path)
        {
            ScreenshotPath = path;
            SetColors();
        }
        /// <summary>
        /// Generates a screenshotinstance by an existing Bitmap
        /// </summary>
        /// <param name="bitmapForScreenshot">screenshot data</param>
        /// <param name="path">belongs to bitmap</param>
        public Screenshot(Bitmap bitmapForScreenshot, string path)
        {
            ScreenshotPath = path;
            SetColors(bitmapForScreenshot);
        }
        /// <summary>
        /// Calculates the color per pixel components by the given parameters
        /// If only the perPixel values are relevant (for unit-testing):
        /// set the color parameter to the desired value and the pixel to 1
        /// than the absolut color component values are equal the perPixel values
        /// </summary>
        /// <param name="absBlue">the sum of all blue values in the screenshot</param>
        /// <param name="absGreen">the sum of all green values in the screenshot</param>
        /// <param name="absRed">the sum of all red values in the screenshot</param>
        /// <param name="pixels">screenshot width * heigth</param>
        public Screenshot(int absBlue, int absGreen, int absRed, int pixels)
        {
            BlueScreenshotComponent = absBlue;
            GreenScreenshotComponent = absGreen;
            RedScreenshotComponent = absRed;
            Pixels = pixels;

            SumOfBlGrReComponents = BlueScreenshotComponent + GreenScreenshotComponent + RedScreenshotComponent;
            SetComponentPerPixelColors();
        }

        /// <summary>
        /// Return an Screenshot intance with the difference of the screenshotparameter
        /// </summary>
        /// <param name="a">minuend</param>
        /// <param name="b">subtrahend</param>
        /// <returns></returns>
        public static Screenshot operator -(Screenshot a, Screenshot b) => a != null && b != null ? Subtract(a, b) : null;
        /// <summary>
        /// Return an Screenshot intance with the difference of the screenshotparameter
        /// </summary>
        /// <param name="left">minuend</param>
        /// <param name="right">subtrahend</param>
        /// <returns></returns>
        public static Screenshot Subtract(Screenshot left, Screenshot right)
        {
            if( left == null )
            {
                throw new ArgumentNullException(nameof(left));
            }
            if( right == null)
            {
                throw new ArgumentNullException(nameof(right));
            }
            return new Screenshot(left.BlueScreenshotComponent - right.BlueScreenshotComponent, left.GreenScreenshotComponent - right.GreenScreenshotComponent, left.RedScreenshotComponent - right.RedScreenshotComponent, left.Pixels > 0 ? left.Pixels : right.Pixels);
        }

        /// <summary>
        /// Iterates over all pixels
        /// Sums the colorvalues and sets calculated average colors
        /// </summary>
        /// <param name="bitmap">gererate color values by an existing bitmap instance</param>
        public unsafe void SetColors(Bitmap bitmap = null)
        {
            using Bitmap screenshot = bitmap ?? new Bitmap(ScreenshotPath) ;

            BitmapData bData = screenshot.LockBits(new Rectangle(0, 0, screenshot.Width, screenshot.Height), ImageLockMode.ReadWrite, screenshot.PixelFormat);

            int bitsPerPixel = 32;

            /*This time we convert the IntPtr to a ptr*/
            byte* scan0 = (byte*)bData.Scan0.ToPointer();

            for (int i = 0; i < bData.Height; ++i)
            {
                for (int j = 0; j < bData.Width; ++j)
                {
                    byte* data = scan0 + i * bData.Stride + j * bitsPerPixel / 8;

                    //data is a pointer to the first byte of the 3-byte color data
                    BlueScreenshotComponent += data[0];
                    GreenScreenshotComponent += data[1];
                    RedScreenshotComponent += data[2];
                }
            }

            screenshot.UnlockBits(bData);

            SumOfBlGrReComponents = BlueScreenshotComponent + GreenScreenshotComponent + RedScreenshotComponent;
            SetComponentPerPixelColors();
        }

        /// <summary>
        /// Sets the PerPixel color data when the total values and pixels setted
        /// </summary>
        void SetComponentPerPixelColors()
        {
            if (Pixels > 0)
            {
                SumOfBlGrReComponentsPerPixel = SumOfBlGrReComponents / Pixels;
                GreenComponentPerPixel = GreenScreenshotComponent / Pixels;
                BlueComponentPerPixel = BlueScreenshotComponent / Pixels;
                RedComponentPerPixel = RedScreenshotComponent / Pixels;
            }
        }

    }


}
