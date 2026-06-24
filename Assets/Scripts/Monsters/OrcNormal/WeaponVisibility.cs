using UnityEngine;

public class WeaponVisibility : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private GameObject handLeft;
    [SerializeField] private GameObject handRight;
    [SerializeField] private GameObject weapon;

    // Llamar desde OrcNormal_Idle (frame donde aparece el arma)
    public void ShowWeapon()
    {
        handLeft.SetActive(true);
        handRight.SetActive(true);
        weapon.SetActive(true);
    }

    // Llamar desde OrcNormal_Run (frame donde desaparecen manos + arma)
    public void HideWeapon()
    {
        handLeft.SetActive(false);
        handRight.SetActive(false);
        weapon.SetActive(false);
    }

    // Llamar desde OrcNormal_Death (frame donde deben desaparecer las manos)
    public void HideHandsOnDeath()
    {
        handLeft.SetActive(false);
        handRight.SetActive(false);
        weapon.SetActive(false);
    }
}