# SMBFogTool 

This is a tool for importing and exporting fog and fog animation headers to and from SMB2 stagedef files. 

	Usage:

	SMBFogTool -i [source] 				- Extracts fog data from input stagdef to an XML file.
	SMBFogTool -i [source] -o [destination] 	- Copies fog data from source to the destination stagedef.
	SMBFogTool -i [source] -c [XML file names]	- Copies the fog data from XML files.
    
For the third option, Use ONLY the shared file name. For example, for 'test.fog.xml' and 'test.foganim.xml', use 'test'.   

At least one keyframe with identical settings to the header is required for fog to show up in-game.
	
There are 6 types of fog defined in SMB2: 

	    GX_FOG_NONE, GX_FOG_LIN, GX_FOG_EXP, GX_FOG_EXP2, GX_FOG_REVEXP, GX_FOG_REVEXP2

Color is stored as (x, y, z), where x, y, and z represent red, green and blue. Each value of color is stored as a real number from 0 to 1, where 1 is the maximum value. To convert a typical 0-255 RGB color value, simply divide the value by 255.
