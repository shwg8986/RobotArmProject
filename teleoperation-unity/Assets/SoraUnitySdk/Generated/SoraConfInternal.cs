using System.Collections.Generic;
using System.Linq;
namespace SoraConf.Internal
{
    
    [System.Serializable]
    public class DataChannel
    {
        public string label = "";
        public string direction = "";
        public bool enable_ordered;
        public bool ordered;
        public bool enable_max_packet_life_time;
        public int max_packet_life_time;
        public bool enable_max_retransmits;
        public int max_retransmits;
        public bool enable_protocol;
        public string protocol = "";
        public bool enable_compress;
        public bool compress;
        public override bool Equals(object obj)
        {
            var v = obj as DataChannel;
            if (v == null) return false;
            if (!this.label.Equals(v.label)) return false;
            if (!this.direction.Equals(v.direction)) return false;
            if (!this.enable_ordered.Equals(v.enable_ordered)) return false;
            if (!this.ordered.Equals(v.ordered)) return false;
            if (!this.enable_max_packet_life_time.Equals(v.enable_max_packet_life_time)) return false;
            if (!this.max_packet_life_time.Equals(v.max_packet_life_time)) return false;
            if (!this.enable_max_retransmits.Equals(v.enable_max_retransmits)) return false;
            if (!this.max_retransmits.Equals(v.max_retransmits)) return false;
            if (!this.enable_protocol.Equals(v.enable_protocol)) return false;
            if (!this.protocol.Equals(v.protocol)) return false;
            if (!this.enable_compress.Equals(v.enable_compress)) return false;
            if (!this.compress.Equals(v.compress)) return false;
            return true;
        }
        
        public override int GetHashCode()
        {
            int hashcode = 1430287;
            hashcode = hashcode * 7302013 ^ label.GetHashCode();
            hashcode = hashcode * 7302013 ^ direction.GetHashCode();
            hashcode = hashcode * 7302013 ^ enable_ordered.GetHashCode();
            hashcode = hashcode * 7302013 ^ ordered.GetHashCode();
            hashcode = hashcode * 7302013 ^ enable_max_packet_life_time.GetHashCode();
            hashcode = hashcode * 7302013 ^ max_packet_life_time.GetHashCode();
            hashcode = hashcode * 7302013 ^ enable_max_retransmits.GetHashCode();
            hashcode = hashcode * 7302013 ^ max_retransmits.GetHashCode();
            hashcode = hashcode * 7302013 ^ enable_protocol.GetHashCode();
            hashcode = hashcode * 7302013 ^ protocol.GetHashCode();
            hashcode = hashcode * 7302013 ^ enable_compress.GetHashCode();
            hashcode = hashcode * 7302013 ^ compress.GetHashCode();
            return hashcode;
        }
        
    }
    
    [System.Serializable]
    public class ConnectConfig
    {
        public string unity_version = "";
        public List<string> signaling_url = new List<string>();
        public string channel_id = "";
        public string client_id = "";
        public string metadata = "";
        public string role = "";
        public bool enable_multistream;
        public bool multistream;
        public bool enable_spotlight;
        public bool spotlight;
        public int spotlight_number;
        public string spotlight_focus_rid = "";
        public string spotlight_unfocus_rid = "";
        public bool enable_simulcast;
        public bool simulcast;
        public string simulcast_rid = "";
        public int capturer_type;
        public long unity_camera_texture;
        public string video_capturer_device = "";
        public bool video;
        public bool audio;
        public int video_width;
        public int video_height;
        public string video_codec_type = "";
        public int video_bit_rate;
        public bool unity_audio_input;
        public bool unity_audio_output;
        public string audio_recording_device = "";
        public string audio_playout_device = "";
        public string audio_codec_type = "";
        public int audio_bit_rate;
        public bool enable_data_channel_signaling;
        public bool data_channel_signaling;
        public int data_channel_signaling_timeout;
        public bool enable_ignore_disconnect_websocket;
        public bool ignore_disconnect_websocket;
        public int disconnect_wait_timeout;
        public List<global::SoraConf.Internal.DataChannel> data_channels = new List<global::SoraConf.Internal.DataChannel>();
        public bool insecure;
        public string bundle_id = "";
        public string proxy_url = "";
        public string proxy_username = "";
        public string proxy_password = "";
        public string proxy_agent = "";
        public override bool Equals(object obj)
        {
            var v = obj as ConnectConfig;
            if (v == null) return false;
            if (!this.unity_version.Equals(v.unity_version)) return false;
            if (!this.signaling_url.SequenceEqual(v.signaling_url)) return false;
            if (!this.channel_id.Equals(v.channel_id)) return false;
            if (!this.client_id.Equals(v.client_id)) return false;
            if (!this.metadata.Equals(v.metadata)) return false;
            if (!this.role.Equals(v.role)) return false;
            if (!this.enable_multistream.Equals(v.enable_multistream)) return false;
            if (!this.multistream.Equals(v.multistream)) return false;
            if (!this.enable_spotlight.Equals(v.enable_spotlight)) return false;
            if (!this.spotlight.Equals(v.spotlight)) return false;
            if (!this.spotlight_number.Equals(v.spotlight_number)) return false;
            if (!this.spotlight_focus_rid.Equals(v.spotlight_focus_rid)) return false;
            if (!this.spotlight_unfocus_rid.Equals(v.spotlight_unfocus_rid)) return false;
            if (!this.enable_simulcast.Equals(v.enable_simulcast)) return false;
            if (!this.simulcast.Equals(v.simulcast)) return false;
            if (!this.simulcast_rid.Equals(v.simulcast_rid)) return false;
            if (!this.capturer_type.Equals(v.capturer_type)) return false;
            if (!this.unity_camera_texture.Equals(v.unity_camera_texture)) return false;
            if (!this.video_capturer_device.Equals(v.video_capturer_device)) return false;
            if (!this.video.Equals(v.video)) return false;
            if (!this.audio.Equals(v.audio)) return false;
            if (!this.video_width.Equals(v.video_width)) return false;
            if (!this.video_height.Equals(v.video_height)) return false;
            if (!this.video_codec_type.Equals(v.video_codec_type)) return false;
            if (!this.video_bit_rate.Equals(v.video_bit_rate)) return false;
            if (!this.unity_audio_input.Equals(v.unity_audio_input)) return false;
            if (!this.unity_audio_output.Equals(v.unity_audio_output)) return false;
            if (!this.audio_recording_device.Equals(v.audio_recording_device)) return false;
            if (!this.audio_playout_device.Equals(v.audio_playout_device)) return false;
            if (!this.audio_codec_type.Equals(v.audio_codec_type)) return false;
            if (!this.audio_bit_rate.Equals(v.audio_bit_rate)) return false;
            if (!this.enable_data_channel_signaling.Equals(v.enable_data_channel_signaling)) return false;
            if (!this.data_channel_signaling.Equals(v.data_channel_signaling)) return false;
            if (!this.data_channel_signaling_timeout.Equals(v.data_channel_signaling_timeout)) return false;
            if (!this.enable_ignore_disconnect_websocket.Equals(v.enable_ignore_disconnect_websocket)) return false;
            if (!this.ignore_disconnect_websocket.Equals(v.ignore_disconnect_websocket)) return false;
            if (!this.disconnect_wait_timeout.Equals(v.disconnect_wait_timeout)) return false;
            if (!this.data_channels.SequenceEqual(v.data_channels)) return false;
            if (!this.insecure.Equals(v.insecure)) return false;
            if (!this.bundle_id.Equals(v.bundle_id)) return false;
            if (!this.proxy_url.Equals(v.proxy_url)) return false;
            if (!this.proxy_username.Equals(v.proxy_username)) return false;
            if (!this.proxy_password.Equals(v.proxy_password)) return false;
            if (!this.proxy_agent.Equals(v.proxy_agent)) return false;
            return true;
        }
        
        public override int GetHashCode()
        {
            int hashcode = 1430287;
            hashcode = hashcode * 7302013 ^ unity_version.GetHashCode();
            foreach (var v in this.signaling_url) hashcode = hashcode * 7302013 ^ v.GetHashCode();
            hashcode = hashcode * 7302013 ^ channel_id.GetHashCode();
            hashcode = hashcode * 7302013 ^ client_id.GetHashCode();
            hashcode = hashcode * 7302013 ^ metadata.GetHashCode();
            hashcode = hashcode * 7302013 ^ role.GetHashCode();
            hashcode = hashcode * 7302013 ^ enable_multistream.GetHashCode();
            hashcode = hashcode * 7302013 ^ multistream.GetHashCode();
            hashcode = hashcode * 7302013 ^ enable_spotlight.GetHashCode();
            hashcode = hashcode * 7302013 ^ spotlight.GetHashCode();
            hashcode = hashcode * 7302013 ^ spotlight_number.GetHashCode();
            hashcode = hashcode * 7302013 ^ spotlight_focus_rid.GetHashCode();
            hashcode = hashcode * 7302013 ^ spotlight_unfocus_rid.GetHashCode();
            hashcode = hashcode * 7302013 ^ enable_simulcast.GetHashCode();
            hashcode = hashcode * 7302013 ^ simulcast.GetHashCode();
            hashcode = hashcode * 7302013 ^ simulcast_rid.GetHashCode();
            hashcode = hashcode * 7302013 ^ capturer_type.GetHashCode();
            hashcode = hashcode * 7302013 ^ unity_camera_texture.GetHashCode();
            hashcode = hashcode * 7302013 ^ video_capturer_device.GetHashCode();
            hashcode = hashcode * 7302013 ^ video.GetHashCode();
            hashcode = hashcode * 7302013 ^ audio.GetHashCode();
            hashcode = hashcode * 7302013 ^ video_width.GetHashCode();
            hashcode = hashcode * 7302013 ^ video_height.GetHashCode();
            hashcode = hashcode * 7302013 ^ video_codec_type.GetHashCode();
            hashcode = hashcode * 7302013 ^ video_bit_rate.GetHashCode();
            hashcode = hashcode * 7302013 ^ unity_audio_input.GetHashCode();
            hashcode = hashcode * 7302013 ^ unity_audio_output.GetHashCode();
            hashcode = hashcode * 7302013 ^ audio_recording_device.GetHashCode();
            hashcode = hashcode * 7302013 ^ audio_playout_device.GetHashCode();
            hashcode = hashcode * 7302013 ^ audio_codec_type.GetHashCode();
            hashcode = hashcode * 7302013 ^ audio_bit_rate.GetHashCode();
            hashcode = hashcode * 7302013 ^ enable_data_channel_signaling.GetHashCode();
            hashcode = hashcode * 7302013 ^ data_channel_signaling.GetHashCode();
            hashcode = hashcode * 7302013 ^ data_channel_signaling_timeout.GetHashCode();
            hashcode = hashcode * 7302013 ^ enable_ignore_disconnect_websocket.GetHashCode();
            hashcode = hashcode * 7302013 ^ ignore_disconnect_websocket.GetHashCode();
            hashcode = hashcode * 7302013 ^ disconnect_wait_timeout.GetHashCode();
            foreach (var v in this.data_channels) hashcode = hashcode * 7302013 ^ v.GetHashCode();
            hashcode = hashcode * 7302013 ^ insecure.GetHashCode();
            hashcode = hashcode * 7302013 ^ bundle_id.GetHashCode();
            hashcode = hashcode * 7302013 ^ proxy_url.GetHashCode();
            hashcode = hashcode * 7302013 ^ proxy_username.GetHashCode();
            hashcode = hashcode * 7302013 ^ proxy_password.GetHashCode();
            hashcode = hashcode * 7302013 ^ proxy_agent.GetHashCode();
            return hashcode;
        }
        
    }
    
}
