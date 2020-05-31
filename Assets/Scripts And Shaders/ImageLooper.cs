using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ImageLooper : MonoBehaviour 
{
    public List<Texture2D> images;

    public TextAsset whoCSVFile;

    public List<Transform> cumulativeCasesPillarForRegion;
    Dictionary<string, Transform> cumulativeCasesByRegionName = new Dictionary<string, Transform>();
    public List<Transform> cumulativeDeathsPillarForRegion;
    Dictionary<string, Transform> cumulativeDeathsByRegionName = new Dictionary<string, Transform>();

    public GameObject issOrbit;
    public GameObject countryPrefab;
    public GameObject startButton;

    public Text currentDate;

    public string firstTextureName = "_MainTex";
    public string secondTextureName = "_NextTex";

    public float timeScale = 1f;

    public Material mainMaterial;
    public Transform regionsParent;

    void Start()
    {
        startTime = -1;

        ImportWHOData();

        cumulativeCasesByRegionName.Clear();
        foreach (var region in cumulativeCasesPillarForRegion)
            cumulativeCasesByRegionName.Add(region.name, region);
        cumulativeDeathsByRegionName.Clear();
        foreach (var region in cumulativeDeathsPillarForRegion)
            cumulativeDeathsByRegionName.Add(region.name, region);

        InitializeCountryPillars();
    }

    int imageIndex;
    float startTime = -1;
    float lerpFactor;
    void Update () 
    {
        if (startTime < 0)
            return;

        float time_f = (Time.time-startTime) * timeScale;
        imageIndex = (int)time_f;
        //imageIndex = time % (images.Count-1);
        issOrbit.SetActive(imageIndex > 66);
        UpdateCameraView();
        if (imageIndex >= images.Count - 1)
            return;
        lerpFactor = time_f - imageIndex;
        mainMaterial.SetTexture(firstTextureName, images[imageIndex]);
        mainMaterial.SetTexture(secondTextureName, images[imageIndex + 1]);
        mainMaterial.SetFloat("_LerpFactor", lerpFactor);

        currentDate.text = images[imageIndex].name;

        UpdateWHOData();
	}

    Vector3 viewportPointForCameraRay = new Vector3(0.5f, 0.5f, 0);
    RaycastHit hit;
    public Text pillarInfo;
    void UpdateCameraView()
    {
        pillarInfo.text = "";
        var ray = Camera.main.ViewportPointToRay(viewportPointForCameraRay);
        if (Physics.Raycast(ray, out hit))
        {
            if (hit.transform.name == "Earth")
            {
                pillarInfo.text = "View from orbit\nPurple pillars: cases, orange pillar: deaths";
            }
            else if (hit.transform.parent != null && hit.transform.parent.parent != null)
            {
                var possibleCountryName = hit.transform.parent.name;
                var possibleRegionName = hit.transform.parent.parent.name;
                if (
                    countryDataByRegionByTime[currentDay].TryGetValue(possibleRegionName, out Dictionary<string, WHOData> countryData) &&
                    countryData.TryGetValue(possibleCountryName, out WHOData currentData))
                {
                    pillarInfo.text = "Region: " + possibleRegionName + ", country: " + possibleCountryName + "\nCases: " + currentData.cumulativeCases + ", deaths: " + currentData.cumulativeDeaths;
                }
            }
        }
    }

    public class WHOData
    {
        public int newCases;
        public int cumulativeCases;
        public int newDeaths;
        public int cumulativeDeaths;

        public WHOData(int casesNew, int casesCumulative, int deathsNew, int deathsCumulative)
        {
            newCases = casesNew;
            cumulativeCases = casesCumulative;
            newDeaths = deathsNew;
            cumulativeDeaths = deathsCumulative;
        }
    }

    SortedDictionary<string, Dictionary<string, WHOData>> regionalDataByTime = new SortedDictionary<string, Dictionary<string, WHOData>>();
    SortedDictionary<string, Dictionary<string, Dictionary<string, WHOData>>> countryDataByRegionByTime = new SortedDictionary<string, Dictionary<string, Dictionary<string, WHOData>>>();
    public List<string> times = new List<string>();
    public List<string> regions = new List<string>();
    Dictionary<string, List<string>> countryByRegion = new Dictionary<string, List<string>>();
    [ContextMenu("Import CSV file")]
    public void ImportWHOData()
    {
        regions.Clear();
        times.Clear();
        countryByRegion.Clear();
        regionalDataByTime.Clear();
        countryDataByRegionByTime.Clear();

        var columns = whoCSVFile.text.Split('\n');
        Debug.Log(columns.Length.ToString());
        foreach (var row in columns)
        {
            if (row == "")
                continue;
            var cells = row.Split(',');
            if (cells[3] == "")
                continue;
            var time = cells[0].Substring(0, 10);
            if (!times.Contains(time))
                times.Add(time);
            if (!regions.Contains(cells[3]))
                regions.Add(cells[3]);
            if (!countryByRegion.ContainsKey(cells[3]))
                countryByRegion.Add(cells[3], new List<string>());
            if (!countryByRegion[cells[3]].Contains(cells[2]))
                countryByRegion[cells[3]].Add(cells[2]);

        }

        foreach (var time in times)
        {
            regionalDataByTime.Add(time,
                new Dictionary<string, WHOData>());
            countryDataByRegionByTime.Add(time,
                new Dictionary<string, Dictionary<string, WHOData>>());
            foreach (var region in regions)
            {
                regionalDataByTime[time].Add(region,
                    new WHOData(0, 0, 0, 0));
                countryDataByRegionByTime[time].Add(region,
                    new Dictionary<string, WHOData>());
            }
        }

        foreach (var row in columns)
        {
            if (row == "")
                continue;
            var cells = row.Split(',');
            if (cells[3] == "")
                continue;
            var time = cells[0].Substring(0, 10);
            regionalDataByTime[time][cells[3]].newCases += int.Parse(cells[4]);
            regionalDataByTime[time][cells[3]].cumulativeCases += int.Parse(cells[5]);
            regionalDataByTime[time][cells[3]].newDeaths += int.Parse(cells[6]);
            regionalDataByTime[time][cells[3]].cumulativeDeaths += int.Parse(cells[7]);

            countryDataByRegionByTime[time][cells[3]].Add(cells[2],
                new WHOData(int.Parse(cells[4]), int.Parse(cells[5]), int.Parse(cells[6]), int.Parse(cells[7])));
        }
    }

    string currentDay;
    Vector3 fixedScaleValues = new Vector3(0, 1, 1);
    Vector3 fixedPillarValues = new Vector3(1, 0, 1);
    void UpdateWHOData()
    {
        currentDay = images[imageIndex].name;
        if ( regionalDataByTime.TryGetValue(currentDay, out Dictionary<string,WHOData> current))
        {
            foreach (var region in current)
            {
                if (cumulativeCasesByRegionName.ContainsKey(region.Key))
                    cumulativeCasesByRegionName[region.Key].localScale = Vector3.Lerp(
                        cumulativeCasesByRegionName[region.Key].localScale,
                        Vector3.right * region.Value.cumulativeCases * 0.0018f + fixedScaleValues,
                        lerpFactor);

                if (cumulativeDeathsByRegionName.ContainsKey(region.Key))
                    cumulativeDeathsByRegionName[region.Key].localScale = Vector3.Lerp(
                        cumulativeDeathsByRegionName[region.Key].localScale,
                        Vector3.right * region.Value.cumulativeDeaths * 0.0018f + fixedScaleValues,
                        lerpFactor);

                if (countryDataByRegionByTime[currentDay].TryGetValue(region.Key, out Dictionary<string, WHOData> countryData))
                {
                    foreach (var country in countryData)
                    {
                        if (countryPillars.ContainsKey(country.Key))
                        {
                            countryPillars[country.Key].gameObject.SetActive(country.Value.cumulativeDeaths > 0);
                            countryPillars[country.Key].localScale = Vector3.Lerp(
                                countryPillars[country.Key].localScale,
                                Vector3.up * country.Value.cumulativeDeaths + fixedPillarValues,
                                lerpFactor);
                        }
                    }
                }
            }
        }
    }

    public List<Transform> countryPillarParents;
    Dictionary<string, Transform> countryPillars = new Dictionary<string, Transform>();
    void InitializeCountryPillars()
    {
        countryPillars.Clear();
        foreach (var parent in countryPillarParents)
        {
            if (countryByRegion.TryGetValue(parent.name, out List<string> countries))
            {
                var pillarsPerRow = countries.Count > 30 ? 8 : 4;
                for (int i = 0; i < countries.Count; i++)
                {
                    var pillar = GameObject.Instantiate(countryPrefab);
                    pillar.name = countries[i];
                    countryPillars.Add(pillar.name, pillar.transform);
                    pillar.transform.SetParent(parent);
                    pillar.transform.localPosition =
                        Vector3.right * (i % pillarsPerRow) +
                        Vector3.forward * (i / pillarsPerRow);
                }
            }
        }
    }

    public void StartPresentation()
    {
        startButton.SetActive(false);
        startTime = Time.time;
    }
}
