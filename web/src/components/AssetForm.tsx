import { useState } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { api } from '../lib/api'
import { ASSET_CATEGORIES, type Asset, type AssetCategory, type AssetInput } from '../lib/types'
import { Button, ErrorMessage, Field, Input, Modal, Select } from './ui'

/** Admin form to add a new asset or edit an existing one. */
export function AssetForm({ asset, onClose, onSaved }: { asset?: Asset; onClose: () => void; onSaved?: (a: Asset) => void }) {
  const queryClient = useQueryClient()
  const [form, setForm] = useState<AssetInput>({
    tag: asset?.tag ?? '',
    name: asset?.name ?? '',
    category: asset?.category ?? 'Laptop',
    serialNumber: asset?.serialNumber ?? '',
    location: asset?.location ?? '',
    purchasedOn: asset?.purchasedOn ?? '',
  })

  const save = useMutation({
    mutationFn: () => {
      const input: AssetInput = {
        ...form,
        serialNumber: form.serialNumber?.trim() || null,
        location: form.location?.trim() || null,
        purchasedOn: form.purchasedOn || null,
      }
      return asset ? api.updateAsset(asset.id, input) : api.createAsset(input)
    },
    onSuccess: (saved) => {
      queryClient.invalidateQueries({ queryKey: ['assets'] })
      queryClient.invalidateQueries({ queryKey: ['asset', saved.id] })
      onSaved?.(saved)
      onClose()
    },
  })

  const set = (key: keyof AssetInput) => (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>) =>
    setForm({ ...form, [key]: e.target.value })

  return (
    <Modal title={asset ? `Edit ${asset.tag}` : 'Add asset'} onClose={onClose}>
      <form
        className="space-y-4"
        onSubmit={(e) => {
          e.preventDefault()
          save.mutate()
        }}
      >
        <div className="grid grid-cols-2 gap-3">
          <Field label="Tag" hint="e.g. LAP-0005">
            <Input value={form.tag} onChange={set('tag')} required maxLength={20} />
          </Field>
          <Field label="Category">
            <Select value={form.category} onChange={(e) => setForm({ ...form, category: e.target.value as AssetCategory })}>
              {ASSET_CATEGORIES.map((c) => (
                <option key={c}>{c}</option>
              ))}
            </Select>
          </Field>
        </div>
        <Field label="Name">
          <Input value={form.name} onChange={set('name')} required maxLength={150} placeholder="Dell Latitude 7450" />
        </Field>
        <div className="grid grid-cols-2 gap-3">
          <Field label="Serial number">
            <Input value={form.serialNumber ?? ''} onChange={set('serialNumber')} maxLength={100} />
          </Field>
          <Field label="Purchased on">
            <Input type="date" value={form.purchasedOn ?? ''} onChange={set('purchasedOn')} />
          </Field>
        </div>
        <Field label="Location">
          <Input value={form.location ?? ''} onChange={set('location')} maxLength={100} placeholder="IT Store, Level 3" />
        </Field>
        <ErrorMessage error={save.error} />
        <div className="flex justify-end gap-2">
          <Button type="button" variant="secondary" onClick={onClose}>
            Cancel
          </Button>
          <Button type="submit" disabled={save.isPending}>
            {asset ? 'Save changes' : 'Add asset'}
          </Button>
        </div>
      </form>
    </Modal>
  )
}
