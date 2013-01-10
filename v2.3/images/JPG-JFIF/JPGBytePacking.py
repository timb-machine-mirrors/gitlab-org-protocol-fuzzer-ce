from Peach.transformer import Transformer

class BytePacking(Transformer):
        '''
        Byte packing 0xFF to 0xFF 0x00
        '''
        
        def realEncode(self, data):
            return data.replace("\xff\x00","\xff").replace("\xff","\xff\x00")
        
        def realDecode(self, data):
                return data.replace("\xff\x00","\xff")
