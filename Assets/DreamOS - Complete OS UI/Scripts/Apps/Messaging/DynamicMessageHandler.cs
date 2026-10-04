using System.Collections;
using UnityEngine;

namespace Michsky.DreamOS
{
    public class DynamicMessageHandler : MonoBehaviour
    {
        [HideInInspector] public MessagingManager manager;
        GameObject messageTimerObject;

        public IEnumerator HandleDynamicMessage(float timer, int layoutIndex)
        {
            yield return new WaitForSeconds(timer);

            GameObject timerObj = Instantiate(manager.chatMessageTimer, new Vector3(0, 0, 0), Quaternion.identity) as GameObject;
            ChatLayoutPreset layout = manager.chatViewer.Find(manager.chatList[layoutIndex].chatTitle).GetComponent<ChatLayoutPreset>();
            timerObj.transform.SetParent(layout.messageParent, false);

            messageTimerObject = timerObj;
            StartCoroutine(FinishDynamicMessage(manager.chatList[layoutIndex].chatAsset.dynamicMessages[manager.dynamicMessageIndex].replyTimer, layoutIndex));
        }

        IEnumerator FinishDynamicMessage(float timer, int layoutIndex)
        {
            yield return new WaitForSeconds(timer);

            manager.allowInputSubmit = true;
            manager.CreateDynamicMessage(layoutIndex, false);

            if (manager.chatList[layoutIndex].chatAsset.dynamicMessages[manager.dynamicMessageIndex].replyBehavior == MessagingChat.DynamicMessageReplyBehavior.DisableReply)
            {
                manager.chatList[layoutIndex].chatAsset.dynamicMessages[manager.dynamicMessageIndex].enableReply = false;
            }

            Destroy(messageTimerObject);
            Destroy(gameObject);
        }

        public IEnumerator HandleStoryTeller(float timer, int layoutIndex, bool isIndividual)
        {
            yield return new WaitForSeconds(timer);

            GameObject timerObj = Instantiate(manager.chatMessageTimer, new Vector3(0, 0, 0), Quaternion.identity);
            ChatLayoutPreset layout = manager.chatViewer.Find(manager.chatList[layoutIndex].chatTitle).GetComponent<ChatLayoutPreset>();
            timerObj.transform.SetParent(layout.messageParent, false);

            messageTimerObject = timerObj;
            StartCoroutine(CreateStoryTellerMessage(manager.chatList[layoutIndex].chatAsset.storyTeller[manager.storyTellerIndex].messageTimer, layoutIndex, isIndividual));
        }

        IEnumerator CreateStoryTellerMessage(float timer, int layoutIndex, bool isIndividual)
        {
            yield return new WaitForSeconds(timer);

            Destroy(messageTimerObject);

            // Find the chat layout
            ChatLayoutPreset layout = manager.chatViewer.Find(manager.chatList[layoutIndex].chatTitle).GetComponent<ChatLayoutPreset>();

            // Check for localization key
            string tempLocKey = manager.chatList[layoutIndex].chatAsset.storyTeller[manager.storyTellerIndex].messageKey;

            // Create the message
            if (isIndividual) { manager.CreateCustomIndividualMessage(layout, manager.chatList[layoutIndex].chatAsset.storyTeller[manager.storyTellerIndex].messageContent, manager.GetTimeData(), tempLocKey); }
            else { manager.CreateCustomMessage(layout, manager.chatList[layoutIndex].chatAsset.storyTeller[manager.storyTellerIndex].messageContent, manager.GetTimeData(), tempLocKey); }

            if (manager.stIndexHelper == manager.currentLayout && manager.storyTellerAnimator.gameObject.activeInHierarchy) { manager.ShowStorytellerPanel(); }
            manager.isStoryTellerOpen = true;
        }

        public IEnumerator HandleStoryTellerLatency(float timer, int layoutIndex, int itemIndex)
        {
            yield return new WaitForSeconds(timer);
            StartCoroutine(FinishStoryTeller(manager.chatList[layoutIndex].chatAsset.storyTeller[manager.storyTellerIndex].replies[itemIndex].feedbackTimer, layoutIndex));

            GameObject timerObj = Instantiate(manager.chatMessageTimer, new Vector3(0, 0, 0), Quaternion.identity);
            ChatLayoutPreset layout = manager.chatViewer.Find(manager.chatList[layoutIndex].chatTitle).GetComponent<ChatLayoutPreset>();
            timerObj.transform.SetParent(layout.messageParent, false);

            messageTimerObject = timerObj;
        }

        IEnumerator FinishStoryTeller(float timer, int layoutIndex)
        {
            yield return new WaitForSeconds(timer);
          
            // Find the chat layout
            ChatLayoutPreset layout = manager.chatViewer.Find(manager.chatList[layoutIndex].chatTitle).GetComponent<ChatLayoutPreset>();

            // Check for localization key
            string tempLocKey = manager.chatList[layoutIndex].chatAsset.storyTeller[manager.storyTellerIndex].replies[manager.stItemIndex].feedbackKey;

            // Create the message
            manager.CreateIndividualMessage(layout, manager.chatList[layoutIndex].chatAsset.storyTeller[manager.storyTellerIndex].replies[manager.stItemIndex].replyFeedback, tempLocKey);

            if (!string.IsNullOrEmpty(manager.chatList[layoutIndex].chatAsset.storyTeller[manager.storyTellerIndex].replies[manager.stItemIndex].callAfter))
            {
                manager.CreateStoryTeller(manager.chatList[layoutIndex].chatTitle, manager.chatList[layoutIndex].chatAsset.storyTeller[manager.storyTellerIndex].replies[manager.stItemIndex].callAfter);
            }

            Destroy(messageTimerObject);
            Destroy(gameObject);
        }
    }
}