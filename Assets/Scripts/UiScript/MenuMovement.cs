using UnityEngine;

public class MenuMovement : MonoBehaviour
{
    public void MoveMenu()
    {
        Transform playerLook = GameObject.Find("CenterEyeAnchor").transform;
        Transform playerHand = GameObject.Find("OculusHand_R_Name").transform;
        

        // Hard code the y offset as 1
        float yOffset = 0f;
        float xOffset = 0f; 
        float zOffset = 0.5f; 

        // Move the menu to the player's position with a y offset
        transform.position = new Vector3(playerHand.position.x + xOffset, playerHand.position.y + yOffset, playerHand.position.z + zOffset);

        // Rotate the menu to match the player's rotation
        transform.rotation = playerLook.rotation;
    }

    public void HideMenu()
    {
        transform.position = new Vector3(0, -1000, 0);
    }
}