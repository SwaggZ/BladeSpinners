# Start Screen Asset Hooks

The Start Screen works without final art or audio and uses runtime placeholders until
these assets are supplied.

## Game logo

Add a transparent image at:

`Assets/Resources/UI/GameLogo.png`

The image is loaded by resource name and scaled to fit without changing its aspect ratio.
No scene or inspector reference is required.

## Theme song

Add the MP3 to:

`Assets/SoundEffects/Music/Background`

Add its title and author to `music-metadata.json`, assign its situation to
`StartScreen`, and add a same-name JPG under `Background/Logos`. The build validator
allows exactly one Start Screen theme and the music service loops it until the player
enters the Main Menu. Until it exists, one Main Menu track is used as an audible
temporary theme.

## Transition sound

Add one or more clips to:

`Assets/SoundEffects/GUI/Start Screen Transition`

The folder-driven sound catalog discovers them automatically. One random clip plays
when the player clicks or presses a button to enter the Main Menu.
