using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;

public class CSVLogger : MonoBehaviour
{
    private string csvPath;

    public void Initialize()
    {
        csvPath = Path.Combine(Application.persistentDataPath, "session_log.csv");
        if (!File.Exists(csvPath))
        {
            string header = "Timestamp,Attempt,PainterName,CanvasName,MuscleIndex,MuscleGermanName,MusclePixels,TotalPaintedPixels,CorrectPaintedPixels,OverpaintedPixels,f1Accuracy\n";
            File.WriteAllText(csvPath, header);
        }
    }

    public void SaveRound(PaintStats ps, float accuracy, string painter, string canvas, int muscleIndex, string muscleNameGer, int hintStep)
    {
        string f1Accuracy = accuracy.ToString("F2", CultureInfo.InvariantCulture) + "%";

        string line = $"{System.DateTime.Now.ToString("yyyy_MM_dd-HH_mm_ss")}," +
                      $"{hintStep}," +
                      $"{painter}," +
                      $"{canvas}," +
                      $"{muscleIndex}," +
                      $"{muscleNameGer}," +
                      $"{ps.referenceMaskPixelCount}," +
                      $"{ps.totalPaintedPixels}," +
                      $"{ps.correctPaintedPixels}," +
                      $"{ps.overpaintedPixels}," +
                      $"{f1Accuracy}";

        string path = Path.Combine(Application.persistentDataPath, "session_log.csv");
        File.AppendAllText(path, line + "\n");
    }

}
