using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

public class FeedDatabase : MonoBehaviour
{
    public int resWidth = 600;              //implement automatic call (also for SelecboxMultiTouch_centrefinger.cs), so that the resolution values automatically get read from the source of this info
    public int resHeight = 400;
    public SemiautonomousHandler semiautonomousHandler;
    public RawImage render_RawImage;
    public string coordinates; 
    //private string currentDate;

    public static string FileName(int width, int height, string type, string app, string date)
    {
        if (app == "semiAutonomousDetectItem")
        {
            if (type == "screenshot")
            {
                return string.Format("{0}/semiAutonomousDetectItem/BoxViewArchive/ROSinputSADetect_{1}x{2}_{3}.png",
                                     Application.dataPath,
                                     width, height,
                                     date);
            }

            if (type == "coordinates")
            {
                return string.Format("{0}/semiAutonomousDetectItem/BoxViewArchive/ROSinputSADetect_{1}x{2}_{3}.txt",
                         Application.dataPath,
                         width, height,
                         date);
            }
            else return null;
        }
        else if (app == "semiAutonomousGraspItem")
        {
            if (type == "screenshot")
            {
                return string.Format("{0}/semiAutonomousGraspItem/BoxViewArchive/ROSinputSAGrasp_{1}x{2}_{3}.png",
                                     Application.dataPath,
                                     width, height,
                                     date);
            }

            if (type == "coordinates")
            {
                return string.Format("{0}/semiAutonomousGraspItem/BoxViewArchive/ROSinputSAGrasp_{1}x{2}_{3}.txt",
                         Application.dataPath,
                         width, height,
                         date);
            }
            else return null;
        }
        else return null;
    }

    public void WriteToDatabase(string applicationName, string successMessage)
    {
        string currentDate = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        render_RawImage = GetComponent<RawImage>();
        Texture2D screenShot = Instantiate(render_RawImage.texture as Texture2D);
        byte[] bytes = screenShot.EncodeToPNG();
        string filename = FileName(resWidth, resHeight, "screenshot", applicationName, currentDate);
        System.IO.File.WriteAllBytes(filename, bytes);
        string textcontent = semiautonomousHandler.Coordinates + " " + successMessage + " " + currentDate;
        filename = FileName(resWidth, resHeight, "coordinates", applicationName, currentDate);
        StreamWriter outputFile = new StreamWriter(filename);
        outputFile.WriteLine(textcontent);
        outputFile.Close();
        semiautonomousHandler.Coordinates = "";
        return;
    }
}