c_port = 45685
c_checksum_length = 4

ct_channel, ct_fragment, ct_ping, ct_pong = 0,1,2,3
c_type_names = 
{
    [ct_channel] = "Channel",
    [ct_fragment] = "Fragment",
    [ct_ping] = "Ping",
    [ct_pong] = "Pong",
}

local lunet = Proto.new("lunet", "Lunet Protocol")
lunet.fields = { }

-- Channel
local field_channel_id = ProtoField.uint8("lunet.channel_id", "Channel ID", base.DEC)
local field_channel_data = ProtoField.bytes("lunet.channel_data", "Channel Data")
table.insert(lunet.fields, field_channel_id)
table.insert(lunet.fields, field_channel_data)
function handle_channel(buffer, offset, length, lunet_tree, data)
    local current_offset = offset

    local id = buffer(current_offset, 1)
    current_offset = current_offset + id:len()

    local data = buffer(current_offset, length - (current_offset - offset))
    current_offset = current_offset + data:len()

    local fragment_tree = lunet_tree:add(buffer(offset, length), "Channel: " .. id:le_uint() .. ", Length: " .. data:len())
    fragment_tree:add_le(field_channel_id, id)
    fragment_tree:add(field_channel_data, data)
end

-- Fragment
local field_fragment_seq = ProtoField.uint16("lunet.fragment_seq", "Fragment Sequence", base.DEC)
local field_fragment_count = ProtoField.uint8("lunet.fragment_count", "Fragment Count", base.DEC)
local field_fragment_index = ProtoField.uint8("lunet.fragment_index", "Fragment Index", base.DEC)
local field_fragment_data = ProtoField.bytes("lunet.fragment_data", "Fragment Data")
table.insert(lunet.fields, field_fragment_seq)
table.insert(lunet.fields, field_fragment_count)
table.insert(lunet.fields, field_fragment_index)
table.insert(lunet.fields, field_fragment_data)
function handle_fragment(buffer, offset, length, lunet_tree, data)
    local current_offset = offset

    local seq = buffer(current_offset, 2)
    current_offset = current_offset + seq:len()

    local count = buffer(current_offset, 1)
    current_offset = current_offset + count:len()

    local index = buffer(current_offset, 1)
    current_offset = current_offset + index:len()

    local data = buffer(current_offset, length - (current_offset - offset))
    current_offset = current_offset + data:len()

    local fragment_tree = lunet_tree:add(buffer(offset, length), "Fragment: " .. seq:le_uint() .. " (" .. (index:le_uint() + 1) .. "/" .. count:le_uint() .. "), Length: " .. data:len())
    fragment_tree:add_le(field_fragment_seq, seq)
    fragment_tree:add_le(field_fragment_count, count)
    fragment_tree:add_le(field_fragment_index, index)
    fragment_tree:add(field_fragment_data, data)
end

-- Ping
local field_ping_seq = ProtoField.uint16("lunet.ping_seq", "Ping Sequence", base.DEC)
table.insert(lunet.fields, field_ping_seq)
function handle_ping(buffer, offset, length, lunet_tree, data)
    lunet_tree:add_le(field_ping_seq, buffer(offset, 2))
end

-- Pong
local field_pong_seq = ProtoField.uint16("lunet.pong_seq", "Pong Sequence", base.DEC)
table.insert(lunet.fields, field_pong_seq)
function handle_pong(buffer, offset, length, lunet_tree, data)
    lunet_tree:add_le(field_pong_seq, buffer(offset, 2))
end


-- Protocol
local field_peer = ProtoField.string("lunet.peer", "Peer")
local field_packet = ProtoField.uint8("lunet.packet", "Packet", base.DEC, c_type_names)
local field_checksum = ProtoField.uint32("lunet.checksum", "Checksum", base.HEX)
table.insert(lunet.fields, field_peer)
table.insert(lunet.fields, field_packet)
table.insert(lunet.fields, field_checksum)
function lunet.dissector(buffer, pinfo, tree)
    local lunet_tree = tree:add(lunet, buffer())
    local length = buffer:len()

    -- Peer
    local peer = "Server"
    if pinfo.src_port ~= c_port then
        peer = "Client"
    end
    lunet_tree:add(field_peer, peer)

    -- Packet
    local packet_buffer = buffer(0, 1)
    local packet_value = packet_buffer:uint()
    lunet_tree:add_le(field_packet, packet_buffer)

    -- Info
    pinfo.cols.protocol = "Lunet"
    pinfo.cols.info = "Packet: " .. (c_type_names[packet_value] or "<Unknown>") .. ", Length: " .. buffer:len()

    -- Packet functions
    local packet_func_buffer_offset = 1
    local packet_func_buffer_length = length - c_checksum_length - packet_func_buffer_offset
    local packet_funcs = 
    {
        [ct_channel] = handle_channel,
        [ct_fragment] = handle_fragment,
        [ct_ping] = handle_ping,
        [ct_pong] = handle_pong,
    }
    local packet_func = packet_funcs[packet_value]
    if packet_func then
        packet_func(buffer, packet_func_buffer_offset, packet_func_buffer_length, lunet_tree)
    end

    -- Checksum
    local checksum_buffer = buffer(length - c_checksum_length, c_checksum_length)
    lunet_tree:add(field_checksum, checksum_buffer)
end

DissectorTable.get("udp.port"):add(c_port, lunet)
