function onInteractMessage(message,player){
		return 0;
}
function requiresInput(){
	return false;
}
function getEntry(player){
	if(npstate){
		if(!npstate.i){
			npstate.i=0;
		}
		SendMessage(roomid,"API MESSAGE!!!");
		return "Np state exists "+(++npstate.i) ;
	}else{
		npstate.i = 0;
		return "Np state doesn't exist";
		
	}
	
}
