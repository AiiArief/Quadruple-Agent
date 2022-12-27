/// ----------------------------------------------------------------------
/// <summary>
///     Bullet that instantiated by player on shoot
/// </summary>
/// <author>Narendra Arief Nugraha</author>
/// ----------------------------------------------------------------------
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletController : MonoBehaviour
{
    [SerializeField] Rigidbody m_rigidbody;

    [Tooltip("Bullet's move speed in meter / second")]
    [SerializeField] float m_bulletSpeed = 50.0f;

    #region Unity's Callback
    private void OnCollisionEnter(Collision collision)
    {
        var player = collision.gameObject.GetComponent<PlayerController>();
        if (player)
            player.photonView.RPC("_HitByBulletRPC", Photon.Pun.RpcTarget.AllViaServer);

        Destroy(gameObject);
    }
    #endregion

    /// <summary>
    ///     Call this after instantiate to make this bullet works!
    /// </summary>
    /// <param name="originalDirection">Where are you going?</param>
    /// <param name="lag">Photon server time minus here</param>
    public void InitializeBullet(Vector3 originalDirection, float lag)
    {
        Destroy(gameObject, 3.0f);
        transform.forward = originalDirection;

        m_rigidbody.position += m_rigidbody.velocity * lag;
        m_rigidbody.velocity = originalDirection * m_bulletSpeed;
    }
}
