# TerminPlannerBOT

# [Invite](https://discord.com/api/oauth2/authorize?client_id=739149559250288702&permissions=93248&scope=bot)

# Commands
!help 
>Prints a list of Commands

!change prefix <prefix>
>Changes the Command Prefix for this Server

!set termin channel <id>
>Sets the channel where the termins will occour

!set termin channel 
> Sets the (current)channel where the termins will occour

!termin list 
>Lists all termins of the server

!termin add <name> <date&time> [optional parameter]
>description: <description>

>Adds a termin

!termin modify <id> [optional parameter]
>name: <name>
description: <description>
dateandtime: <date&time>

>Modifys a existing termin

!termin remove <terminId>
>removes a termin

!termin reset <terminId>
>resets the reactions of a termin

## optional parameter

#### parameterName: parametervalue

## text with spaces
#### "A text with spaces has to bee between quotes"

# Host the BOT yourself

## config.xml (in same directory as excecutable)

```xml
<?xml version="1.0" encoding="utf-8"?>

<config>
	<token>yourToken</token>
	<defaultPrefix>!</defaultPrefix>
	<savePath relativePath="true" path="Saves\"/>
	<website name="documentation" url="https://github.com/DerSemmel/TerminPlannerBOT"/>
</config>
```

