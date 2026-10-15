using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SecretsReborn
{
    [Serializable] public sealed class DamageNumberEvent
    {
        public long sequence;
        public int areaEpoch, amount;
        public string worldId, scene;
        public float x,y,z;
        public bool critical;
    }
    public sealed partial class NetworkCoop
    {
        private long damageSequence, receivedDamageSequence;
        public static void PresentDamage(Vector3 position,int amount,bool critical)
        {
            if (IsReplica || amount<=0) return;
            DamageNumbersView.Show(position,amount,critical);
            if (!Running || !Active.manager.IsServer || Active.ChangingArea) return;
            var net=Active;
            var data=new DamageNumberEvent { sequence=++net.damageSequence, areaEpoch=net.areaEpoch,
                worldId=GameSession.Instance.World.WorldId, scene=SceneManager.GetActiveScene().path,
                amount=amount, critical=critical, x=position.x,y=position.y,z=position.z };
            foreach (var client in net.owners.Keys)
                if (client!=NetworkManager.ServerClientId) net.Send("sr.damage",client,JsonUtility.ToJson(data));
        }
        private void ReceiveDamageNumber(ulong sender,FastBufferReader reader)
        {
            if (manager.IsServer || sender!=NetworkManager.ServerClientId || ChangingArea || GameSession.Instance.Busy || reader.Length>1024) return;
            try
            {
                reader.ReadValueSafe(out string json); var data=JsonUtility.FromJson<DamageNumberEvent>(json);
                if (data==null || data.sequence<=receivedDamageSequence || data.areaEpoch!=areaEpoch || data.amount<=0
                    || data.worldId!=GameSession.Instance.World.WorldId || data.scene!=SceneManager.GetActiveScene().path
                    || float.IsNaN(data.x) || float.IsInfinity(data.x) || float.IsNaN(data.y) || float.IsInfinity(data.y)
                    || float.IsNaN(data.z) || float.IsInfinity(data.z)) return;
                receivedDamageSequence=data.sequence;
                DamageNumbersView.Show(new Vector3(data.x,data.y,data.z),data.amount,data.critical);
            }
            catch (Exception) { /* Malformed presentation messages never alter gameplay state. */ }
        }
    }
}
