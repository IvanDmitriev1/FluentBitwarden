#include <pch.h>
#include "CborWriter.h"
#include <cbor-lite/codec.h>

void CborWriter::WriteMap(std::size_t itemCount)
{
    m_size += CborLite::encodeMapSize(
        m_buffer,
        static_cast<std::uint64_t>(itemCount));
}

void CborWriter::WriteArrayHeader(std::size_t itemCount)
{
    m_size += CborLite::encodeArraySize(
        m_buffer,
        static_cast<std::uint64_t>(itemCount));
}

void CborWriter::WriteKey(std::uint64_t value)
{
    WriteUnsigned(value);
}

void CborWriter::WriteUnsigned(std::uint64_t value)
{
    m_size += CborLite::encodeUnsigned(
        m_buffer,
        static_cast<std::uint64_t>(value));
}

void CborWriter::WriteInteger(std::int64_t value)
{
    m_size += CborLite::encodeInteger(
        m_buffer,
        static_cast<std::int64_t>(value));
}

void CborWriter::WriteBool(bool value)
{
    m_size += CborLite::encodeBool(m_buffer, value);
}

void CborWriter::WriteText(std::string_view text)
{
    m_size += CborLite::encodeText(m_buffer, ToTextBuffer(text));
}

void CborWriter::WriteBytes(std::span<const std::uint8_t> bytes)
{
    m_size += CborLite::encodeBytes(m_buffer, ToByteBuffer(bytes));
}

void CborWriter::WriteTextArray(std::span<const std::string_view> values)
{
    WriteArrayHeader(values.size());

    for (const auto value : values)
    {
        WriteText(value);
    }
}


std::vector<std::uint8_t> CborWriter::Finish()
{
    return std::vector<std::uint8_t>(m_buffer.begin(), m_buffer.end());
}

std::vector<char> CborWriter::ToTextBuffer(std::string_view text)
{
    return std::vector<char>(text.begin(), text.end());
}

std::vector<CborWriter::Byte> CborWriter::ToByteBuffer(std::span<const std::uint8_t> bytes)
{
    return std::vector<Byte>(bytes.begin(), bytes.end());
}
