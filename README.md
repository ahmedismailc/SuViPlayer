# SuVi Player

[![GitHub release (latest by date)](https://img.shields.io/github/v/release/ahmedismailc/SuViPlayer)](https://github.com/ahmedismailc/SuViPlayer/releases/latest)
[![License](https://img.shields.io/github/license/ahmedismailc/SuViPlayer)](LICENSE)

SuVi Player is a free and open-source video player for Windows, meticulously crafted to supercharge your language learning and provide an unparalleled subtitle-focused viewing experience.  It goes *far* beyond basic playback, offering a suite of powerful, interactive tools.

## Overview

While SuVi Player excels at facilitating pronunciation practice through techniques like shadowing, it's also a versatile media player designed for *anyone* who wants to engage deeply with subtitles. Whether you're a language learner, a movie enthusiast, or someone who relies on subtitles for accessibility, SuVi Player offers a unique and feature-rich experience.

## Key Features:

**Subtitle Powerhouse:**

*   **Interactive Subtitle Panel:**
    *   Displays current subtitle in a dedicated, resizable panel.
    *   Supports multi-line subtitles and text selection.
    *   Right-click context menu:
        *   **Copy:** Copy selected text.
        *   **Save to My List:** Add words/phrases to your vocabulary list.
        *   **Customize:** Manage custom dictionary/lookup links.
        *   **Instant Dictionary Lookups:**  Click to search selected words in online dictionaries (Google Translate, Youglish, and more – fully customizable!).
*   **My List (Vocabulary Builder):**
    *   Save words/phrases directly from subtitles.
    *   Review saved words, grouped by date, in a dedicated "My List" window.
    *   Copy all, today's, or selected words to the clipboard.
    *   Clear your list with ease.
*   **Customizable Dictionary Links:**
    *   Add, edit, delete, and reorder your own lookup links (dictionaries, search engines, etc.).
    *   Use the `{word}` placeholder for dynamic lookups.
    *   Default links included; reset option available.
*   **Embedded Subtitle Extraction:**
    *   Automatically extracts subtitles from video files (if no external .srt is found).
    *   Uses FFmpeg (with an automatic download prompt if needed).
    *   Subtitle track selection for videos with multiple subtitle streams.
    *   Option to save extracted subtitles as .srt files.
* **Non-SRT Subtitle Support(Experimental):** Add and convert Non-SRT Subtitle.
*   **Add Subtitles to Playing Video:** Load .srt files on-the-fly.
*   **Subtitle Cleaning**: Automatically cleans up subtitles.
*   **Precise Subtitle Synchronization:**  Fine-tuned timing and navigation.

**Playback & Learning Tools:**

*   **A-B Loop:** Define a specific video segment for focused, repeated playback.  Great for mastering difficult sections.
*   **Repeat After Me:** Automatically pause playback after each subtitle, providing time for repetition and practice.  The pause duration is intelligently calculated.
*   **Adjustable Playback Speed:**  Control playback speed from 0.25x to 2.0x.
*   **Audio Track Selection:**  Choose from available audio tracks in videos with multiple streams.
*   **Negative Time Display:**  Toggle the total time display to show remaining time.
*   **Hide Cursor:** Hides Cursor During playback.

**General Features:**

*   **Intuitive Interface:** Clean, user-friendly design with a `MenuStrip` and context menus.
*   **Extensive Keyboard Shortcuts:**  Control playback, subtitles, and more with customizable shortcuts (documented in a dedicated window).
*    New Shortcuts `T` key to show/hide subtitle panel, `R` key to toggle `Repeat After Me`.
*   **Drag-and-Drop Support:**  Open video and .srt files by dragging them onto the application.
*   **Automatic Update Checker:**  Checks for new releases on GitHub (at startup and manually).
*   **Dark Mode Support:**  A visually comfortable dark theme.
*   **Open Source:**  Freely available and modifiable under [GPL-3.0 license].

**Supported Formats:**

SuVi Player leverages the power of LibVLCSharp and FFmpeg, providing broad support for a wide range of video and subtitle formats, including (but not limited to):

*   **Video:** MP4, MKV, AVI, MOV, WMV, FLV, WEBM
*   **Subtitles:** SRT, VTT, ASS, SSA, SUB
*    **Audio:** Multiple audio tracks

**💡 Ideal for:**

*   Language learners of *all* levels.
*   Practicing pronunciation and shadowing.
*   Building vocabulary.
*   Studying transcripts and dialogue.
*   Anyone who relies on subtitles for accessibility.
*   Movie and TV show enthusiasts who want a powerful subtitle experience.

**Built With:**

*   C#
*   .NET
*   LibVLCSharp
*   FFmpeg
*   SQLite

**Getting Started:**

1.  **Download:** Grab the latest release from the [Releases](https://github.com/ahmedismailc/SuViPlayer/releases) page.
2.  **Install:** Follow the installation instructions (if any – it might be a portable application).
3.  **Launch:** Start SuVi Player and open a video file.
4.  **Explore:** Discover the features and customize the settings to your liking!


**License:**

This project is licensed under the [GPL-3.0 license] - see the [LICENSE](LICENSE) file for details.

**Arabic demo video:**
https://youtu.be/KQnnF23SJYU

**SuVi Player v1.0.0.0 Screenshot:**
![Screenshot 2024-11-06 042910](https://github.com/user-attachments/assets/27ab942e-b1fc-4caa-8f7c-705f42ef613f)
