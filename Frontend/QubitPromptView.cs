/*
 * Qubit Menu  Frontend/QubitPromptView.cs
 * A community driven mod menu for Gorilla Tag with over 1000+ mods
 *
 * Copyright (C) 2026  Qubit Software  /Derivative work of Seralyth Software, Goldentrophy Software
 * https://github.com/QubitGT/Qubit
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */
using System;

using Qubit.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace Qubit.Frontend
{
    public sealed class QubitPromptView : MonoBehaviour
    {
        private TMP_Text message, input, pageLabel;
        private RawImage media;
        private VideoPlayer video;
        private RenderTexture videoTexture;
        private int page = 1;
        private bool textInput;

        public static QubitPromptView Attach(Transform front, Prompts.PromptData data)
        {
            var panel = front.Find("Prompt");
            panel.gameObject.SetActive(true);
            var view = panel.gameObject.AddComponent<QubitPromptView>();
            view.message = panel.Find("Message").GetComponent<TMP_Text>();
            view.input = panel.Find("Input/Value").GetComponent<TMP_Text>();
            view.pageLabel = panel.Find("Page").GetComponent<TMP_Text>();
            view.media = panel.Find("Media").GetComponent<RawImage>();
            view.textInput = data.IsText;
            panel.Find("Title").GetComponent<TMP_Text>().text = data.IsText ? "Enter a response" : data.DeclineText == null ? "Notice" : "Confirmation";
            view.message.text = data.Message ?? "";
            panel.Find("Input").gameObject.SetActive(data.IsText);
            panel.Find("Accept/Label").GetComponent<TMP_Text>().text = data.AcceptText ?? "OK";
            panel.Find("Decline/Label").GetComponent<TMP_Text>().text = data.DeclineText ?? "";
            panel.Find("Decline").gameObject.SetActive(data.DeclineText != null);
            if (data.DeclineText == null)
            {
                ((RectTransform)panel.Find("Accept")).sizeDelta = new Vector2(484, 48);
                ((RectTransform)panel.Find("Accept/Label")).sizeDelta = new Vector2(480, 48);
            }
            var url = Prompts.ExtractPromptImage(data.Message);
            if (url != null)
            {
                view.message.text = view.message.text.Replace("<" + url + ">", "");
                view.message.rectTransform.sizeDelta = new Vector2(484, 144);
                view.LoadMedia(url);
            }
            Canvas.ForceUpdateCanvases();
            view.RefreshPages();
            return view;
        }

        public void Browse(int delta)
        {
            page = Mathf.Clamp(page + delta, 1, Math.Max(1, message.textInfo.pageCount));
            RefreshPages();
        }

        private void RefreshPages()
        {
            message.ForceMeshUpdate();
            int pages = Math.Max(1, message.textInfo.pageCount);
            page = Mathf.Clamp(page, 1, pages);
            message.pageToDisplay = page;
            pageLabel.text = pages > 1 ? page + " / " + pages : "";
            transform.Find("Previous").gameObject.SetActive(pages > 1);
            transform.Find("Next").gameObject.SetActive(pages > 1);
        }

        private void LoadMedia(string url)
        {
            try
            {
                var uri = new Uri(url);
                string filename = System.IO.Path.GetFileName(uri.AbsolutePath);
                string extension = System.IO.Path.GetExtension(filename).ToLowerInvariant();
                if (extension == ".mat") media.material = Prompts.promptMaterial;
                else if (extension == ".mp4" || extension == ".webm" || extension == ".mov")
                {
                    videoTexture = new RenderTexture(768, 432, 0);
                    videoTexture.Create();
                    media.texture = videoTexture;
                    video = gameObject.AddComponent<VideoPlayer>();
                    video.playOnAwake = false;
                    video.isLooping = true;
                    video.renderMode = VideoRenderMode.RenderTexture;
                    video.targetTexture = videoTexture;
                    video.aspectRatio = VideoAspectRatio.FitInside;
                    video.url = url;
                    Prompts.promptVideoPlayer = video;
                    video.Play();
                }
                else
                {
                    media.texture = Qubit.Utilities.Assets.LoadTextureFromURL(url, filename);
                    if (media.texture != null)
                    {
                        float ratio = (float)media.texture.width / media.texture.height;
                        var size = ratio > 484f / 260f ? new Vector2(484, 484 / ratio) : new Vector2(260 * ratio, 260);
                        media.rectTransform.sizeDelta = size;
                        media.rectTransform.anchoredPosition = new Vector2((484 - size.x) / 2, -214);
                    }
                }
                media.gameObject.SetActive(true);
            }
            catch (Exception error) { Qubit.Libraries.Core.Logs.LogError("Prompt media could not load: " + error); }
        }

        private void LateUpdate()
        {
            if (textInput) input.text = string.IsNullOrEmpty(Keyboard.keyboardInput)
                ? "Type using the keyboard…" : Keyboard.keyboardInput + (Time.unscaledTime % 1 < .5f ? "|" : "");
        }

        private void OnDestroy()
        {
            if (video != null) video.Stop();
            if (Prompts.promptVideoPlayer == video) Prompts.promptVideoPlayer = null;
            if (videoTexture != null) { videoTexture.Release(); Destroy(videoTexture); }
        }
    }
}
