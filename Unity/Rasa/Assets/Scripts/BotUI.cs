﻿using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// This class contains the gameobjects and methods for interacting with the UI.
/// </summary>
public class BotUI : MonoBehaviour {

    [Header("Chatbot GameObjects & References")]
    public GameObject       ChatbotUI;                          // reference to all canvas chatbot objects 
    public GameObject       userBubble;                         // reference to user chat bubble prefab
    public GameObject       botBubble;                          // reference to bot chat bubble prefab
    public RuntimeAnimatorController    
                            chatAnimatorController;             // reference to chat bubble animation controller
    public NetworkManager   networkManager;                     // reference to Network Manager script
    public SelectionWheel   selectionWheel;                     // reference to Selection Wheel script

    [Header("User Input GameObjects & References")]
    public GameObject       contentDisplayObject;               // Gameobject where all the conversation is shown
    public InputField       inputField;                         // InputField gameobject wher user types their message

    [HideInInspector]
    public bool             renderingMessage;                   // bool to keep track of message while animation is running
    public static bool      botUIActive = false;                // bool to keep track of whether the bot should be shown or not
    private const int       messagePadding = 15;                // space between chat bubbles 
    private int             allMessagesHeight = messagePadding; // int to keep track of where next message should be rendered


    /// <summary>
    /// This method recieves the array of messages from <see cref="NetworkManager.RecieveMessage(string)">
    /// and prints them on botUI with an animation.
    /// </summary>
    /// <param name="recieveMessages">A list of messages received from the bot</param>
    /// <returns></returns>
    public IEnumerator UnpackMessagesAfterDelay (RootMessages recieveMessages) {
        // show message based on message type on UI
        foreach (RecieveData message in recieveMessages.messages) {
            FieldInfo[] fields = typeof(RecieveData).GetFields();
            foreach (FieldInfo field in fields) {
                string data = null;

                // extract data from response in try-catch for handling null exceptions
                try {
                    data = field.GetValue(message).ToString();
                } catch (NullReferenceException) { }

                // print data
                if (data != null && field.Name != "recipient_id") {
                    // Create chat bubble, add animator and reposition
                    GameObject chatBubbleChild = CreateChatBubble("bot");
                    AddChatComponent(chatBubbleChild, "", "animation");
                    StartCoroutine(SetChatBubblePosition(chatBubbleChild.transform.parent.GetComponent<RectTransform>(), "bot"));
                    
                    // Add delay then remove animator and update message height counter
                    yield return new WaitForSeconds(2f);
                    allMessagesHeight -= messagePadding + (int)chatBubbleChild.transform.parent.GetComponent<RectTransform>().sizeDelta.y;
                    if (chatBubbleChild.GetComponent<Animator>() != null) {
                        Destroy(chatBubbleChild.GetComponent<Animator>());
                        Destroy(chatBubbleChild.GetComponent<Image>());
                    }
                    yield return new WaitForEndOfFrame();

                    // Add message to the chat bubble
                    UpdateDisplay(data, field.Name, chatBubbleChild);
                }
            }
        }
    }

    /// <summary>
    /// This method is used to update the display panel with the user's messages by creating message bubble
    /// </summary>
    /// <param name="sender">The one who wrote this message</param>
    /// <param name="message">The message</param>
    /// <param name="messageType">The message type like text, image etc</param>
    public void UpdateDisplay (string sender, string message, string messageType) {
        // Create chat bubble and show animation
        GameObject chatBubbleChild = CreateChatBubble(sender);

        // Add chat component
        AddChatComponent(chatBubbleChild, message, messageType);

        // Set chat bubble position
        StartCoroutine(SetChatBubblePosition(chatBubbleChild.transform.parent.GetComponent<RectTransform>(), sender));
    }

    /// <summary>
    /// This method is used to update the display panel with the bot's messages by taking message bubble as argument
    /// </summary>
    /// <param name="message">The message</param>
    /// <param name="messageType">The message type like text, image etc</param>
    /// <param name="chatBubbleChild">reference to message bubble gameobject</param>
    public void UpdateDisplay (string message, string messageType, GameObject chatBubbleChild) {
        // Add chat component
        AddChatComponent(chatBubbleChild, message, messageType);

        // Set chat bubble position
        StartCoroutine(SetChatBubblePosition(chatBubbleChild.transform.parent.GetComponent<RectTransform>(), "bot"));
    }

    /// <summary>
    /// Coroutine to set the position of the chat bubble inside the contentDisplayObject.
    /// </summary>
    /// <param name="chatBubblePos">RectTransform of chat bubble</param>
    /// <param name="sender">Sender who sent the message</param>
    private IEnumerator SetChatBubblePosition (RectTransform chatBubblePos, string sender) {
        // Wait for end of frame before calculating UI transform
        yield return new WaitForEndOfFrame();

        // get horizontal position based on sender
        int horizontalPos = 0;
        if (sender == "user") {
            horizontalPos = -50;
        } else if (sender == "bot") {
            horizontalPos = 50;
        }

        // set the vertical position of chat bubble
        allMessagesHeight += messagePadding + (int)chatBubblePos.sizeDelta.y;
        chatBubblePos.anchoredPosition3D = new Vector3(horizontalPos, -allMessagesHeight, 0);

        // enable scrolling by increasing content viewport size if messages exceed a limit
        if (allMessagesHeight > 340) {
            // update contentDisplayObject hieght
            RectTransform contentRect = contentDisplayObject.GetComponent<RectTransform>();
            contentRect.sizeDelta = new Vector2(contentRect.sizeDelta.x, allMessagesHeight + messagePadding);
            contentDisplayObject.transform.GetComponentInParent<ScrollRect>().verticalNormalizedPosition = 0;
        }
    }

    /// <summary>
    /// Coroutine to update chat bubble positions based on their size.
    /// </summary>
    public IEnumerator RefreshChatBubblePosition () {
        // Wait for end of frame before calculating UI transform
        yield return new WaitForEndOfFrame();

        // refresh position of all gameobjects based on size
        int localAllMessagesHeight = messagePadding;
        foreach (RectTransform chatBubbleRect in contentDisplayObject.GetComponent<RectTransform>()) {
            if (chatBubbleRect.sizeDelta.y < 35) {
                localAllMessagesHeight += 35 + messagePadding;
            } else {
                localAllMessagesHeight += (int)chatBubbleRect.sizeDelta.y + messagePadding;
            }
            chatBubbleRect.anchoredPosition3D =
                    new Vector3(chatBubbleRect.anchoredPosition3D.x, -localAllMessagesHeight, 0);
        }

        // Update global message Height variable
        allMessagesHeight = localAllMessagesHeight;
        if (allMessagesHeight > 340) {
            // update contentDisplayObject hieght
            RectTransform contentRect = contentDisplayObject.GetComponent<RectTransform>();
            contentRect.sizeDelta = new Vector2(contentRect.sizeDelta.x, allMessagesHeight + messagePadding);
            contentDisplayObject.transform.GetComponentInParent<ScrollRect>().verticalNormalizedPosition = 0;
        }
    }

    /// <summary>
    /// This method creates chat bubbles from prefabs and sets their positions.
    /// </summary>
    /// <param name="sender">The sender of message for which bubble is rendered</param>
    /// <returns>Reference to empty gameobject on which message components can be added</returns>
    private GameObject CreateChatBubble (string sender) {
        GameObject chat = null;
        if (sender == "user") {
            // Create user chat bubble from prefabs and set it's position
            chat = Instantiate(userBubble);
            chat.transform.SetParent(contentDisplayObject.transform, false);
        } else if (sender == "bot") {
            // Create bot chat bubble from prefabs and set it's position
            chat = Instantiate(botBubble);
            chat.transform.SetParent(contentDisplayObject.transform, false);
        }

        // Add content size fitter
        ContentSizeFitter chatSize = chat.AddComponent<ContentSizeFitter>();
        chatSize.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        chatSize.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Add vertical layout group
        VerticalLayoutGroup verticalLayout = chat.AddComponent<VerticalLayoutGroup>();
        if (sender == "user") {
            verticalLayout.padding = new RectOffset(10, 20, 5, 5);
        } else if (sender == "bot") {
            verticalLayout.padding = new RectOffset(20, 10, 5, 5);
        }
        verticalLayout.childAlignment = TextAnchor.MiddleCenter;

        // Return empty gameobject on which chat components will be added
        return chat.transform.GetChild(0).gameObject;
    }

    /// <summary>
    /// This method adds message component to chat bubbles based on message type.
    /// </summary>
    /// <param name="chatBubbleObject">The empty gameobject under chat bubble</param>
    /// <param name="message">message to be shown</param>
    /// <param name="messageType">The type of message (text, image etc)</param>
    private void AddChatComponent (GameObject chatBubbleObject, string message, string messageType) {        
        switch (messageType) {
            case "animation":
                // animate bot reply
                chatBubbleObject.AddComponent<Image>();
                Animator chatAnimator = chatBubbleObject.AddComponent<Animator>();
                chatAnimator.runtimeAnimatorController = chatAnimatorController;
                break;
            case "text":
                // Create and init Text component
                Text chatMessage = chatBubbleObject.AddComponent<Text>();
                // format message so that each line is at max 50 characters
                message = FormatMessage(message);
                // add font as it is none at times when creating text component from script
                chatMessage.font = Resources.GetBuiltinResource(typeof(Font), "Arial.ttf") as Font;
                chatMessage.fontSize = 18;
                chatMessage.alignment = TextAnchor.MiddleLeft;
                chatMessage.text = message;
                break;
            case "image":
                // Create and init Image component
                Image chatImage = chatBubbleObject.AddComponent<Image>();
                StartCoroutine(networkManager.SetImageTextureFromUrl(message, chatImage));
                break;
            case "attachment":
                break;
            case "buttons":
                break;
            case "elements":
                break;
            case "quick_replies":
                break;
        }
    }

    /// <summary>
    /// This method formats the message to be rendered on the bot ui so that each
    /// line is at max 50 characters.
    /// </summary>
    /// <param name="message">The message to be formatted</param>
    /// <returns>The formatted message</returns>
    private string FormatMessage (string message) {
        // init  variables
        string formatted_message = "";
        int counter = 0;

        //Debug.Log("original message is : " + message);

        // add newline after every 50 characters
        for (int i = 0; i < message.Length; i++) {
            if (counter < 50) {
                formatted_message += message[i];
            } else {
                // add hyphen if a word is being broken
                if (message[i - 1] != ' ') {
                    formatted_message += "-";
                }

                counter = 0;
                formatted_message += "\n";

                formatted_message += message[i];
            }
            counter++;
        }

        //Debug.Log("formatted message is : " + formatted_message);

        // return formatted message string
        return formatted_message;
    }

    /// <summary>
    /// This coroutine makes the chatbot UI window scroll down.
    /// </summary>
    /// <param name="direction">direction in which object will scroll. 0 for Down, 1 for Up</param>
    private IEnumerator ScrollAnimation (int direction) {
        RectTransform chatbotRectTransform = ChatbotUI.GetComponent<RectTransform>();
        //print("scroll animation, direction is : " + direction + " chatbotrect values : " + chatbotRectTransform.offsetMax + ", " + chatbotRectTransform.offsetMin);
        //
        if (direction == 0) {
            while (chatbotRectTransform.offsetMax.y > -175f) {
                yield return new WaitForSeconds(0.01f);
                chatbotRectTransform.offsetMax = new Vector2(chatbotRectTransform.offsetMax.x, chatbotRectTransform.offsetMax.y - 10f);
            }
            // activate input field and set focus after animation
            ActivateInputField();
        } else if (direction == 1) {
            while (chatbotRectTransform.offsetMax.y < 675) {
                yield return new WaitForSeconds(0.01f);
                chatbotRectTransform.offsetMax = new Vector2(chatbotRectTransform.offsetMax.x, chatbotRectTransform.offsetMax.y + 10f);
            }
            // clear previous messages, hide chatbot UI after animation and activate pokemon selection wheel gameobjects
            ClearChabotUIMessages();
            ChatbotUI.SetActive(false);
            selectionWheel.ReactivateSelectionWheelGameObjects();
        }
    }

    /// <summary>
    /// This method clears previous chatbot messages from UI and re-initializes necessary variables
    /// </summary>
    public void ClearChabotUIMessages () {
        // reset hieght of all message objects
        allMessagesHeight = messagePadding;
        // delete all chatbubbles
        foreach (GameObject botBubble in GameObject.FindGameObjectsWithTag("botBubble")) {
            Destroy(botBubble.gameObject);
        }
        foreach (GameObject userBubble in GameObject.FindGameObjectsWithTag("userBubble")) {
            Destroy(userBubble.gameObject);
        }
    }

    /// <summary>
    /// This method checks if Enter key is pressed while input field is active and 
    /// sends message if true
    /// </summary>
    public void Update () {
        if (inputField.IsActive() && Input.GetKeyDown(KeyCode.Return)) {
            networkManager.SendMessageToRasa();
            ActivateInputField();
        }
    }

    /// <summary>
    /// This method sets input field in focus and activates it
    /// </summary>
    private void ActivateInputField () {
        inputField.Select();
        inputField.ActivateInputField();
    }

    public void CloseChatbotUI () {
        botUIActive = false;
    }

    /// <summary>
    /// If bot is online set input field active else inactive
    /// </summary>
    private void LateUpdate () {
        // show and hide chatbot
        if (botUIActive && !ChatbotUI.activeSelf) {
            ChatbotUI.SetActive(true);
            StartCoroutine(ScrollAnimation(0));
        } else if (!botUIActive && ChatbotUI.activeSelf) {
            StartCoroutine(ScrollAnimation(1));
        }

        // toggle input field based on whether bot is online
        if (networkManager.botOnline) {
            inputField.interactable = true;
        } else {
            inputField.interactable = false;
        }
    }
}
