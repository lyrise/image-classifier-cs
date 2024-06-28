# Image Classifier

Image Classifier is a desktop application that allows you to easily classify images. It supports both Windows and Linux platforms and helps streamline the organization and classification of your images.

![Demo](/docs/demo.png)

## Features

- Simple and intuitive user interface
- Fast operation with multiple shortcut keys
- Supports image movement and Undo functionality

## Getting Started

### Installation

Download the latest release from the link below.

- [Download for Windows and Linux](https://github.com/lyrise/image-classifier-cs/releases)

### Launching the Application

#### Debian and Ubuntu

To launch the application on Debian and Ubuntu, set the necessary environment variables as follows:

```sh
export LANG=en_US.UTF-8
SCALE=2
SCREEN=$(xrandr --listactivemonitors | awk -F " " '{ printf("%s", $4) }')
export AVALONIA_SCREEN_SCALE_FACTORS="$SCREEN=$SCALE"
```

## How to Use

1. **Load Function**
   Click the Load button to upload the images you want to classify.

2. **Undo Function**
   Click the Undo button or press the `Up Arrow` key or `K` key to undo the last action.

3. **Right Function**
   Click the Right button or press the `Right Arrow` key or `L` key to move the selected image to the `Right:` directory.

4. **Left Function**
   Click the Left button or press the `Left Arrow` key or `H` key to move the selected image to the `Left:` directory.

## License

This project is released under the MIT License. For more details, please refer to the [LICENSE](LICENSE.txt) file.

## Contribution

If you would like to contribute to this project, please contact us through [Issues](https://github.com/omnius-labs/axus-daemon-rs/issues) or [Pull Requests](https://github.com/omnius-labs/axus-daemon-rs/pulls) on GitHub.
