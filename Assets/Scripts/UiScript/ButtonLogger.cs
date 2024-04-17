using UnityEngine;

public class ButtonLogger : MonoBehaviour
{
    public void LogButtonPressed()
    {
        // Logs the name of the GameObject to which this script is attached when the button is pressed
        Debug.Log("Button pressed: " + gameObject.name);
    }

    public void SpawnObject()
    {
        // Correcting the typo in the resource name ("toilet" instead of "toliet")
        GameObject objectPrefab = Resources.Load<GameObject>(gameObject.name);
        Transform player = GameObject.Find("OVRCameraRig 1").transform;
        if (objectPrefab != null)
        {
            // Instantiate the toilet prefab at the specified position
            Vector3 position = new Vector3(5, 1, 5); //technically not needed
            Instantiate(objectPrefab, player.position, Quaternion.identity);
        }
        else
        {
            Debug.LogError("Failed to load the toilet prefab. Make sure it's named correctly and located in a Resources folder.");
        }
    }
}