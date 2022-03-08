using RoR2;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace DropInMultiplayer
{
    internal static class ChatHelper
    {
        private static DropInMultiplayerConfig DropInConfig => DropInMultiplayer.Instance.DropInConfig;

        [Server]
        public static void GreetNewPlayer(NetworkUser joiningUser)
        {
            if (DropInConfig.WelcomeMessage) //If the host man has enabled this config option.
            {
                var message = DropInConfig.CustomWelcomeMessage;
                if (message.Length > 1000)
                {
                    return;
                }
                message = message.ReplaceOnce("{username}", joiningUser.userName);
                message = message.ReplaceOnce("{survivorlist}", string.Join(", ", BodyHelper.GetSurvivorDisplayNames()));
                AddChatMessage(message, 1f);
            }
        }

        public static void AddChatMessage(string message, float time = 0.1f)
        {
            DropInMultiplayer.Instance.StartCoroutine(AddMessageHelper(message, time));
        }

        private static IEnumerator AddMessageHelper(string message, float time)
        {
            yield return new WaitForSeconds(time);
            var chatMessage = new Chat.SimpleChatMessage { baseToken = message };
            Chat.SendBroadcastChat(chatMessage);
        }
    }
}
